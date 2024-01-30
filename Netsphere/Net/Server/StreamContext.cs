// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;

namespace Netsphere.Net;

#pragma warning disable SA1202 // Elements should be ordered by access

public sealed class StreamContext
{
    public enum State
    {// Server: Receiving->Received->(Sent), Client: Receiving->Received
        Receiving,
        Received,
        Sent,
    }

    internal StreamContext(ReceiveTransmission receiveTransmission, ulong dataId)
    {
        this.ReceiveTransmission = receiveTransmission;
        this.DataId = dataId;
    }

    #region FieldAndProperty

    public State CurrentState { get; internal set; }

    internal ReceiveTransmission ReceiveTransmission { get; }

    public ulong DataId { get; }

    public long ReceivedLength { get; internal set; }

    internal int CurrentPosition { get; set; }

    #endregion

    public void Abort()
        => this.ReceiveTransmission.ProcessAbort();

    public Task<(NetResult Result, int Written)> Receive(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => this.ReceiveTransmission.ProcessReceive(this, buffer, cancellationToken);

    public NetResult SendAndForget<TSend>(TSend data, ulong dataId = 0)
    {
        var connection = this.ReceiveTransmission.Connection;
        if (connection.IsClosedOrDisposed)
        {
            return NetResult.Closed;
        }
        else if (this.CurrentState != State.Received)
        {
            return NetResult.Closed;
        }

        if (connection.CancellationToken.IsCancellationRequested)
        {
            return default;
        }

        if (!BlockService.TrySerialize(data, out var owner))
        {
            return NetResult.SerializationError;
        }

        var transmission = connection.TryCreateSendTransmission(this.ReceiveTransmission.TransmissionId);
        if (transmission is null)
        {
            owner.Return();
            return NetResult.NoTransmission;
        }

        this.CurrentState = State.Sent;
        var result = transmission.SendBlock(0, dataId, owner, default);
        owner.Return();
        return result; // SendTransmission is automatically disposed either upon completion of transmission or in case of an Ack timeout.
    }
}
