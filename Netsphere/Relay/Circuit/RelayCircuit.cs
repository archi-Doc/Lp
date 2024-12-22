// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Text;

namespace Netsphere.Relay;

/// <summary>
/// <see cref="RelayCircuit"/> is a primitive class for managing relay circuits.
/// </summary>
public class RelayCircuit
{
    private const int MaxOutgoingSerialRelays = 5;
    private const int MaxIncomingSerialRelays = 1;

    public RelayCircuit(NetTerminal netTerminal, bool incoming)
    {
        this.netTerminal = netTerminal;
        this.incoming = incoming;
    }

    #region FieldAndProperty

    public int NumberOfRelays
        => this.relayNodes.Count;

    internal RelayKey RelayKey
        => this.relayKey;

    private readonly NetTerminal netTerminal;
    private readonly bool incoming;
    private readonly RelayNode.GoshujinClass relayNodes = new();

    private RelayKey relayKey = new();

    #endregion

    public RelayResult AddRelay(AssignRelayBlock assignRelayBlock, AssignRelayResponse assignRelayResponse, ClientConnection clientConnection, bool closeRelayedConnections = true)
    {
        if (clientConnection.DestinationEndpoint.RelayId != 0)
        {
            return RelayResult.InvalidEndpoint;
        }

        var relayId = assignRelayResponse.InnerRelayId;
        using (this.relayNodes.LockObject.EnterScope())
        {
            var result = this.CanAddRelayInternal(relayId, clientConnection.DestinationEndpoint);
            if (result != RelayResult.Success)
            {
                return result;
            }

            this.relayNodes.Add(new(relayId, assignRelayBlock.InnerKeyAndNonce, clientConnection));
            this.ResetRelayKeyInternal();
        }

        if (closeRelayedConnections)
        {
            this.netTerminal.ConnectionTerminal.CloseRelayedConnections();
        }

        return RelayResult.Success;
    }

    public void Clean()
    {
        using (this.relayNodes.LockObject.EnterScope())
        {
            TemporaryList<RelayNode> deleteList = default;
            foreach (var x in this.relayNodes)
            {
                if (!x.IsOpen)
                {// Connection is closed
                    deleteList.Add(x);
                }
            }

            foreach (var x in deleteList)
            {
                x.Remove();
            }
        }
    }

    public void Close(bool closeRelayedConnections = true)
    {
        var numberOfRelays = this.NumberOfRelays;
        var packet = RelayOperatioPacket.CreateClose();
        for (var i = -numberOfRelays; i < 0; i++)
        {
            _ = this.netTerminal.PacketTerminal.SendAndReceive<RelayOperatioPacket, RelayOperatioResponse>(NetAddress.Relay, packet, i);
        }

        using (this.relayNodes.LockObject.EnterScope())
        {
            this.relayNodes.Clear();
            this.ResetRelayKeyInternal();
        }

        if (closeRelayedConnections)
        {
            this.netTerminal.ConnectionTerminal.CloseRelayedConnections();
        }
    }

    public RelayResult CanAddRelay(RelayId relayId, NetEndpoint endpoint)
    {
        using (this.relayNodes.LockObject.EnterScope())
        {
            return this.CanAddRelayInternal(relayId, endpoint);
        }
    }

    public string UnsafeToString()
    {
        var sb = new StringBuilder();
        using (this.relayNodes.LockObject.EnterScope())
        {
            var i = 0;
            foreach (var x in this.relayNodes)
            {
                sb.Append($"{i++}: {x.ToString()} - ");
            }
        }

        return sb.ToString();
    }

    public async Task<string> UnsafeDetailedToString()
    {
        NetEndpoint[] endpointArray;
        using (this.relayNodes.LockObject.EnterScope())
        {
            endpointArray = this.relayNodes.Select(x => x.Endpoint).ToArray();
        }

        var dictionary = new ConcurrentDictionary<int, PingRelayResponse>();
        var cts = new CancellationTokenSource();
        cts.CancelAfter(NetConstants.DefaultPacketTransmissionTimeout);
        var task = Parallel.ForAsync(0, endpointArray.Length, cts.Token, async (i, cancellationToken) =>
        {
            var relayNumber = -(1 + i); // this.incoming ? 0 : -(1 + i);
            var rr = await this.netTerminal.PacketTerminal.SendAndReceive<PingRelayPacket, PingRelayResponse>(NetAddress.Relay, new(), relayNumber, cancellationToken, EndpointResolution.PreferIpv6, this.incoming);
            if (rr.Result == NetResult.Success &&
            rr.Value is { } response)
            {
                dictionary.TryAdd(i, response);
            }
        });

        try
        {
            await task;
        }
        catch
        {
        }

        var sb = new StringBuilder();
        for (var i = 0; i < endpointArray.Length; i++)
        {
            if (dictionary.TryGetValue(i, out var response))
            {
                sb.AppendLine($"{i}: {endpointArray[i].ToString()} {response.ToString()}");
            }
            else
            {
            }
        }

        return sb.ToString();
    }

    /*internal bool TryEncrypt(int relayNumber, NetAddress destination, ReadOnlySpan<byte> content, out BytePool.RentMemory encrypted, out NetEndpoint relayEndpoint)
        => this.relayKey.TryEncrypt(relayNumber, destination, content, out encrypted, out relayEndpoint);*/

    internal async Task Terminate(CancellationToken cancellationToken)
    {
    }

    private void ResetRelayKeyInternal()
    {// using (this.relayNodes.LockObject.EnterScope())
        this.relayKey = new(this.relayNodes);
    }

    private RelayResult CanAddRelayInternal(RelayId relayId, NetEndpoint endpoint)
    {// using (this.relayNodes.LockObject.EnterScope())
        if (endpoint.RelayId != 0)
        {
            return RelayResult.InvalidEndpoint;
        }

        if (this.incoming)
        {// Incoming circuit
            if (this.relayNodes.Count >= MaxIncomingSerialRelays)
            {
                return RelayResult.SerialRelayLimit;
            }
        }
        else
        {// Outgoing circuit
            if (this.relayNodes.Count >= MaxOutgoingSerialRelays)
            {
                return RelayResult.SerialRelayLimit;
            }
        }

        if (this.relayNodes.RelayIdChain.ContainsKey(relayId))
        {
            return RelayResult.DuplicateRelayId;
        }

        if (this.relayNodes.EndpointChain.ContainsKey(endpoint))
        {
            return RelayResult.DuplicateEndpoint;
        }

        return RelayResult.Success;
    }
}
