// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Core;

public abstract class AsyncResponder<TSend, TReceive> : INetResponder
{
    public ulong DataId
        => NetHelper.GetDataId<TSend, TReceive>();

    public virtual TReceive? RespondAsync(TSend value) => default;

    public bool Respond(TransmissionContext transmissionContext)
    {
        if (!TinyhandSerializer.TryDeserialize<TSend>(transmissionContext.Owner.Memory.Span, out var t))
        {
            transmissionContext.Return();
            return false;
        }

        transmissionContext.Return();

        _ = Task.Run(() =>
        {
            this.ServerConnection = transmissionContext.ServerConnection;
            var response = this.RespondAsync(t);
            if (response is not null)
            {
                transmissionContext.SendAndForget(response, this.DataId);
            }
        });

        return true;
    }

    protected ServerConnection ServerConnection { get; private set; } = default!;
}
