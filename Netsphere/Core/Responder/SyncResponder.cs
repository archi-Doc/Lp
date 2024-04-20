// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Core;

public abstract class SyncResponder<TSend, TReceive> : INetResponder
{
    public ulong DataId
        => NetHelper.GetDataId<TSend, TReceive>();

    public virtual TReceive? RespondSync(TSend value) => default;

    public void Respond(TransmissionContext transmissionContext)
    {
        if (!TinyhandSerializer.TryDeserialize<TSend>(transmissionContext.Owner.Memory.Span, out var t))
        {
            transmissionContext.Return();
            transmissionContext.SendResultAndForget(NetResult.DeserializationFailed);
            return;
        }

        transmissionContext.Return();

        transmissionContext.Result = NetResult.UnknownError;
        this.ServerConnection = transmissionContext.ServerConnection;
        var response = this.RespondSync(t);
        if (response is not null)
        {
            transmissionContext.SendAndForget(response, this.DataId);
        }
        else
        {
            transmissionContext.SendResultAndForget(transmissionContext.Result);
        }
    }

    protected ServerConnection ServerConnection { get; private set; } = default!;
}
