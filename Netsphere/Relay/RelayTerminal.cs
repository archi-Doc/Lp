// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Packet;

namespace Netsphere.Relay;

public class RelayTerminal
{
    public RelayTerminal(NetTerminal netTerminal)
    {
        this.netTerminal = netTerminal;
    }

    #region FieldAndProperty

    private readonly NetTerminal netTerminal;
    private readonly RelayNode.GoshujinClass relayNodes = new();

    #endregion

    public async Task<RelayResult> AddRelay(NetNode netNode, CancellationToken cancellationToken = default)
    {
        if (this.relayNodes.SerialLinkChain.Count >= NetConstants.MaxSerialRelays)
        {
            return RelayResult.SerialRelayLimit;
        }

        using (var clientConnection = await this.netTerminal.Connect(netNode, Connection.ConnectMode.NoReuse).ConfigureAwait(false))
        {
            if (clientConnection is null)
            {
                return RelayResult.ConnectionFailure;
            }

            var block = new CreateRelayBlock((ushort)RandomVault.Pseudo.NextUInt32());
            var r = await clientConnection.SendAndReceive<CreateRelayBlock, CreateRelayResponse>(block, CreateRelayBlock.DataId, cancellationToken).ConfigureAwait(false);
            if (r.IsFailure || r.Value is null)
            {
                return RelayResult.ConnectionFailure;
            }
            else if (r.Value.Result != RelayResult.Success)
            {
                return r.Value.Result;
            }

            lock (this.relayNodes.SyncObject)
            {
            }

            return RelayResult.Success;
        }
    }

    internal async Task Terminate(CancellationToken cancellationToken)
    {
    }
}
