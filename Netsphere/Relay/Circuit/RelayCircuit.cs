// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Text;

namespace Netsphere.Relay;

/// <summary>
/// <see cref="RelayCircuit"/> is a primitive class for managing relay circuits.
/// </summary>
public class RelayCircuit
{
    public RelayCircuit(NetTerminal netTerminal, IRelayControl relayControl)
    {
        this.netTerminal = netTerminal;
        this.relayControl = relayControl;
    }

    #region FieldAndProperty

    public bool IsRelayAvailable
        => this.relayNodes.Count > 0;

    public int NumberOfRelays
        => this.relayNodes.Count;

    internal RelayKey RelayKey
        => this.relayKey;

    private readonly NetTerminal netTerminal;
    private readonly IRelayControl relayControl;
    private readonly RelayNode.GoshujinClass relayNodes = new();

    private RelayKey relayKey = new();

    #endregion

    public RelayResult AddRelay(ushort relayId, ClientConnection clientConnection, bool closeRelayedConnections = true)
    {
        if (clientConnection.DestinationEndpoint.RelayId != 0)
        {
            return RelayResult.InvalidEndpoint;
        }

        lock (this.relayNodes.SyncObject)
        {
            var result = this.CanAddRelayInternal(relayId, clientConnection.DestinationEndpoint);
            if (result != RelayResult.Success)
            {
                return result;
            }

            this.relayNodes.Add(new(relayId, clientConnection));
            this.NewRelayKeyInternal();
        }

        if (closeRelayedConnections)
        {
            this.netTerminal.ConnectionTerminal.CloseRelayedConnections();
        }

        return RelayResult.Success;
    }

    public void Clear(bool closeRelayedConnections = true)
    {
        var numberOfRelays = this.NumberOfRelays;
        var packet = RelayOperatioPacket.CreateClose();
        for (var i = -numberOfRelays; i < 0; i++)
        {
            _ = this.netTerminal.PacketTerminal.SendAndReceive<RelayOperatioPacket, RelayOperatioResponse>(NetAddress.Relay, packet, i);
        }

        lock (this.relayNodes.SyncObject)
        {
            this.relayNodes.Clear();
            this.NewRelayKeyInternal();
        }

        if (closeRelayedConnections)
        {
            this.netTerminal.ConnectionTerminal.CloseRelayedConnections();
        }
    }

    public RelayResult CanAddRelay(ushort relayId, NetEndpoint endpoint)
    {
        lock (this.relayNodes.SyncObject)
        {
            return this.CanAddRelayInternal(relayId, endpoint);
        }
    }

    public string UnsafeToString()
    {
        var sb = new StringBuilder();
        lock (this.relayNodes.SyncObject)
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
        lock (this.relayNodes.SyncObject)
        {
            endpointArray = this.relayNodes.Select(x => x.Endpoint).ToArray();
        }

        var dictionary = new ConcurrentDictionary<int, PingRelayResponse>();
        var cts = new CancellationTokenSource();
        cts.CancelAfter(NetConstants.DefaultPacketTransmissionTimeout);
        var task = Parallel.ForAsync(0, endpointArray.Length, cts.Token, async (i, cancellationToken) =>
        {
            var rr = await this.netTerminal.PacketTerminal.SendAndReceive<PingRelayPacket, PingRelayResponse>(NetAddress.Relay, new(), -(1 + i), cancellationToken);
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

    /*internal bool TryEncrypt(int relayNumber, NetAddress destination, ReadOnlySpan<byte> content, out ByteArrayPool.MemoryOwner encrypted, out NetEndpoint relayEndpoint)
        => this.relayKey.TryEncrypt(relayNumber, destination, content, out encrypted, out relayEndpoint);*/

    internal async Task Terminate(CancellationToken cancellationToken)
    {
    }

    private void NewRelayKeyInternal()
    {// lock (this.relayNodes.SyncObject)
        this.relayKey = new(this.relayNodes);
    }

    private RelayResult CanAddRelayInternal(ushort relayId, NetEndpoint endpoint)
    {// lock (this.relayNodes.SyncObject)
        if (endpoint.RelayId != 0)
        {
            return RelayResult.InvalidEndpoint;
        }
        else if (this.relayNodes.Count >= this.relayControl.MaxSerialRelays)
        {
            return RelayResult.SerialRelayLimit;
        }
        else if (this.relayNodes.RelayIdChain.ContainsKey(relayId))
        {
            return RelayResult.DuplicateRelayId;
        }
        else if (this.relayNodes.EndpointChain.ContainsKey(endpoint))
        {
            return RelayResult.DuplicateEndpoint;
        }

        return RelayResult.Success;
    }
}
