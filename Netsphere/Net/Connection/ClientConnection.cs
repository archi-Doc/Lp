﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using Netsphere.Block;
using Netsphere.Net;
using Netsphere.Packet;

namespace Netsphere;

[ValueLinkObject(Isolation = IsolationLevel.Serializable, Restricted = true)]
public sealed partial class ClientConnection : Connection
{
    [Link(Primary = true, Type = ChainType.Unordered, TargetMember = "ConnectionId")]
    [Link(Type = ChainType.Unordered, Name = "OpenEndPoint", TargetMember = "EndPoint")]
    [Link(Type = ChainType.Unordered, Name = "ClosedEndPoint", TargetMember = "EndPoint")]
    [Link(Type = ChainType.LinkedList, Name = "OpenList", AutoLink = false)] // ResponseSystemMics
    [Link(Type = ChainType.LinkedList, Name = "ClosedList", AutoLink = false)] // ClosedSystemMics
    public ClientConnection(PacketTerminal packetTerminal, ConnectionTerminal connectionTerminal, ulong connectionId, NetEndPoint endPoint)
        : base(packetTerminal, connectionTerminal, connectionId, endPoint)
    {
    }

    public override ConnectionState State
    {
        get
        {
            if (this.OpenEndPointLink.IsLinked)
            {
                return ConnectionState.Open;
            }
            else if (this.ClosedEndPointLink.IsLinked)
            {
                return ConnectionState.Closed;
            }
            else
            {
                return ConnectionState.Disposed;
            }
        }
    }

    public async Task<NetResult> Send<TSend>(TSend packet)
        where TSend : ITinyhandSerialize<TSend>
    {
        if (!BlockService.TrySerialize(packet, out var owner))
        {
            return NetResult.SerializationError;
        }

        if (this.NetBase.CancellationToken.IsCancellationRequested)
        {
            return default;
        }

        using (var transmission = await this.CreateTransmission().ConfigureAwait(false))
        {
            if (transmission is null)
            {
                return NetResult.NoTransmission;
            }

            var tcs = new TaskCompletionSource<NetResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var result = transmission.SendBlock(0, 0, owner, tcs, default, default);
            if (result != NetResult.Success)
            {
                return result;
            }

            result = await tcs.Task.WaitAsync(this.ConnectionTerminal.NetBase.CancellationToken).ConfigureAwait(false);
            return result;
        }
    }

    public async Task<(NetResult Result, TReceive? Value)> SendAndReceive<TSend, TReceive>(TSend packet, ulong dataId = 0)
        where TSend : ITinyhandSerialize<TSend>
        where TReceive : ITinyhandSerialize<TReceive>
    {
        if (!BlockService.TrySerialize(packet, out var owner))
        {
            return (NetResult.SerializationError, default);
        }

        if (this.NetBase.CancellationToken.IsCancellationRequested)
        {
            return (NetResult.Canceled, default);
        }

        using (var transmission = await this.CreateTransmission().ConfigureAwait(false))
        {
            if (transmission is null)
            {
                return (NetResult.NoTransmission, default);
            }

            var tcs = new TaskCompletionSource<NetResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            var result = transmission.SendBlock(0, dataId, owner, default, tcs, default);
            if (result != NetResult.Success)
            {
                return (result, default);
            }

            NetResponse response;
            try
            {
                response = await tcs.Task.WaitAsync(this.ConnectionTerminal.NetBase.CancellationToken).ConfigureAwait(false);
                if (response.IsFailure)
                {
                    return (response.Result, default);
                }
            }
            catch
            {
                return (NetResult.Canceled, default);
            }

            if (!BlockService.TryDeserialize<TReceive>(response.Received, out var receive))
            {
                response.Return();
                return (NetResult.DeserializationError, default);
            }

            response.Return();
            return (NetResult.Success, receive);
        }
    }

    public async Task<ReceiveStreamResult> SendAndReceiveStream<TSend>(TSend packet, ulong dataId = 0)
        where TSend : ITinyhandSerialize<TSend>
    {
        if (!BlockService.TrySerialize(packet, out var owner))
        {
            return new(NetResult.SerializationError);
        }

        if (this.NetBase.CancellationToken.IsCancellationRequested)
        {
            return new(NetResult.Canceled);
        }

        using (var transmission = await this.CreateTransmission().ConfigureAwait(false))
        {
            if (transmission is null)
            {
                return new(NetResult.NoTransmission);
            }

            var stream = new ReceiveStream();
            var result = transmission.SendBlock(0, dataId, owner, default, default, stream);
            if (result != NetResult.Success)
            {
                // stream.Dispose();
                return new(result);
            }

            return new(NetResult.Success, stream);
        }
    }

    public async Task<NetStream?> CreateStream(long size)
    {
        if (this.NetBase.CancellationToken.IsCancellationRequested)
        {
            return default;
        }
        else if (this.Agreement.MaxStreamSize < size)
        {
            return default;
        }

        var transmission = await this.CreateTransmission().ConfigureAwait(false);
        if (transmission is null)
        {
            return default;
        }

        var result = transmission.SendStream(0, 0, size, false);
        if (result != NetResult.Success)
        {
            transmission.Dispose();
            return default;
        }

        return default;
        // return transmission;
    }
}
