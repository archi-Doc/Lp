// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Core;

public abstract class SyncResponder<TSend, TReceive> : INetResponder
{
    public ulong DataId
        => NetHelper.GetDataId<TSend, TReceive>();

    public virtual TReceive? RespondSync(TSend value) => default;

    public bool Respond(TransmissionContext transmissionContext)
    {
        if (!TinyhandSerializer.TryDeserialize<TSend>(transmissionContext.Owner.Memory.Span, out var t))
        {
            transmissionContext.Return();
            return false;
        }

        transmissionContext.Return();

        this.ServerConnection = transmissionContext.ServerConnection;
        var response = this.RespondSync(t);
        if (response == null)
        {
            return false;
        }

        transmissionContext.SendAndForget(response, this.DataId);
        return true;
    }

    protected ServerConnection ServerConnection { get; private set; } = default!;
}
