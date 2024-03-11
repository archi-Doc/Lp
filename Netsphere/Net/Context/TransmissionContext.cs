// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Netsphere.Net;

#pragma warning disable SA1401

namespace Netsphere;

public sealed class TransmissionContext
{
    public static TransmissionContext Current => AsyncLocal.Value!;

    internal static AsyncLocal<TransmissionContext?> AsyncLocal = new();

    internal TransmissionContext(ServerConnection serverConnection, uint transmissionId, uint dataKind, ulong dataId, ByteArrayPool.MemoryOwner toBeShared)
    {
        this.ServerConnection = serverConnection;
        this.TransmissionId = transmissionId;
        this.DataKind = dataKind;
        this.DataId = dataId;
        this.Owner = toBeShared;
    }

    #region FieldAndProperty

    public ServerConnection ServerConnection { get; } // => this.ConnectionContext.ServerConnection;

    public uint TransmissionId { get; }

    public uint DataKind { get; } // 0:Block, 1:RPC, 2:Control

    public ulong DataId { get; }

    public ByteArrayPool.MemoryOwner Owner { get; set; }

    public NetResult Result { get; set; }

    public bool IsSent { get; private set; }

    public ReceiveStream? GetReceiveStream()
        => this.receiveStream;

    private ReceiveStream? receiveStream;

    #endregion

    public void Return()
    {
        this.Owner = this.Owner.Return();
        if (this.receiveStream is not null)
        {
            this.receiveStream.Abort();
            this.receiveStream = default;
        }
    }

    public NetResult SendAndForget<TSend>(TSend data, ulong dataId = 0)
    {
        if (!this.ServerConnection.IsActive)
        {
            return NetResult.Closed;
        }
        else if (this.IsSent)
        {
            return NetResult.InvalidOperation;
        }

        if (typeof(TSend) == typeof(NetResult))
        {
            return this.SendAndForget(ByteArrayPool.MemoryOwner.Empty, Unsafe.As<TSend, ulong>(ref data));
        }

        if (!NetHelper.TrySerialize(data, out var owner))
        {
            return NetResult.SerializationError;
        }

        var transmission = this.ServerConnection.TryCreateSendTransmission(this.TransmissionId);
        if (transmission is null)
        {
            owner.Return();
            return NetResult.NoTransmission;
        }

        this.IsSent = true;
        var result = transmission.SendBlock(0, dataId, owner, default);
        owner.Return();
        return result; // SendTransmission is automatically disposed either upon completion of transmission or in case of an Ack timeout.
    }

    public (NetResult Result, SendStream? Stream) GetSendStream(long maxLength, ulong dataId = 0)
    {
        if (!this.ServerConnection.IsActive)
        {
            return (NetResult.Canceled, default);
        }
        else if (!this.ServerConnection.Agreement.CheckStreamLength(maxLength))
        {
            return (NetResult.StreamLengthLimit, default);
        }
        else if (this.IsSent)
        {
            return (NetResult.InvalidOperation, default);
        }

        var transmission = this.ServerConnection.TryCreateSendTransmission(this.TransmissionId);
        if (transmission is null)
        {
            return (NetResult.NoTransmission, default);
        }

        var tcs = new TaskCompletionSource<NetResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        this.IsSent = true;
        var result = transmission.SendStream(maxLength, tcs);
        if (result != NetResult.Success)
        {
            transmission.Dispose();
            return (result, default);
        }

        return (NetResult.Success, new SendStream(transmission, maxLength, dataId));
    }

    /*public async NetTask<NetResult> InternalUpdateAgreement(ulong dataId, CertificateToken<ConnectionAgreement> a1)
    {
        if (!NetHelper.TrySerialize(a1, out var owner))
        {
            return NetResult.SerializationError;
        }

        var response = await this.RpcSendAndReceive(owner, dataId).ConfigureAwait(false);
        owner.Return();

        try
        {
            if (response.Result != NetResult.Success)
            {
                return response.Result;
            }

            if (!NetHelper.TryDeserializeNetResult(response.Value.Memory.Span, out var result))
            {
                return NetResult.DeserializationError;
            }

            if (result == NetResult.Success)
            {
                this.Agreement.AcceptAll(a1.Target);
                this.ApplyAgreement();
            }

            return result;
        }
        finally
        {
            response.Value.Return();
        }
    }

    public async NetTask<NetResult> InternalConnectBidirectionally(ulong dataId, CertificateToken<ConnectionAgreement>? a1)
    {
        if (!NetHelper.TrySerialize(a1, out var owner))
        {
            return NetResult.SerializationError;
        }

        this.PrepareBidirectionally(); // Create the ServerConnection in advance, as packets may not arrive in order.
        var response = await this.RpcSendAndReceive(owner, dataId).ConfigureAwait(false);
        owner.Return();

        try
        {
            if (response.Result != NetResult.Success)
            {
                return response.Result;
            }

            if (!NetHelper.TryDeserializeNetResult(response.Value.Memory.Span, out var result))
            {
                return NetResult.DeserializationError;
            }

            if (result == NetResult.Success)
            {
                this.Agreement.EnableBidirectionalConnection = true;
            }

            return result;
        }
        finally
        {
            response.Value.Return();
        }
    }*/

    /*public (NetResult Result, ReceiveStream? Stream) ReceiveStream(long maxLength)
    {
        if (this.Connection.CancellationToken.IsCancellationRequested)
        {
            return (NetResult.Canceled, default);
        }
        else if (!this.Connection.Agreement.CheckStreamLength(maxLength))
        {
            return (NetResult.StreamLengthLimit, default);
        }
        else if (this.receiveTransmission is null)
        {
            return (NetResult.InvalidOperation, default);
        }

        var stream = new ReceiveStream(this.receiveTransmission, this.DataId, maxLength);
        this.receiveTransmission = default;
        return (NetResult.Success, stream);
    }*/

    internal NetResult SendAndForget(ByteArrayPool.MemoryOwner toBeShared, ulong dataId = 0)
    {
        if (!this.ServerConnection.IsActive)
        {
            return NetResult.Closed;
        }
        else if (this.IsSent)
        {
            return NetResult.InvalidOperation;
        }

        var transmission = this.ServerConnection.TryCreateSendTransmission(this.TransmissionId);
        if (transmission is null)
        {
            return NetResult.NoTransmission;
        }

        this.IsSent = true;
        var result = transmission.SendBlock(0, dataId, toBeShared, default);
        return result; // SendTransmission is automatically disposed either upon completion of transmission or in case of an Ack timeout.
    }

    internal bool CreateReceiveStream(ReceiveTransmission receiveTransmission, long maxLength)
    {
        if (!this.ServerConnection.IsActive)
        {
            return false;
        }
        else if (!this.ServerConnection.Agreement.CheckStreamLength(maxLength))
        {
            return false;
        }
        else if (this.receiveStream is not null)
        {
            return false;
        }

        this.receiveStream = new ReceiveStream(receiveTransmission, this.DataId, maxLength);
        return true;
    }

    /*internal NetResult ForceSendAndForget(ByteArrayPool.MemoryOwner toBeShared, ulong dataId = 0)
    {
        var transmission = this.Connection.TryCreateSendTransmission(this.TransmissionId);
        if (transmission is null)
        {
            return NetResult.NoTransmission;
        }

        this.IsSent = true;
        var result = transmission.SendBlock(0, dataId, toBeShared, default);
        return result; // SendTransmission is automatically disposed either upon completion of transmission or in case of an Ack timeout.
    }*/
}
