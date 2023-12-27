// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;

namespace Netsphere.Server;

public abstract class SyncResponder<TSend, TReceive> : INetResponder
    where TReceive : ITinyhandSerialize<TReceive>
{
    public virtual ulong DataId => BlockService.GetId<TSend, TReceive>();

    public virtual TReceive? RespondSync(TSend value) => default;

    public bool Respond(TransmissionContext transmissionContext)
    {
        if (!TinyhandSerializer.TryDeserialize<TSend>(transmissionContext.Owner.Memory.Span, out var t))
        {
            transmissionContext.Return();
            return false;
        }

        transmissionContext.Return();

        var response = this.RespondSync(t);
        if (response == null)
        {
            return false;
        }

        transmissionContext.SendAndForget(response, this.DataId);
        return true;
    }
}

public abstract class AsyncResponder<TSend, TReceive> : INetResponder
    where TReceive : ITinyhandSerialize<TReceive>
{
    public virtual ulong DataId => BlockService.GetId<TSend, TReceive>();

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
            var response = this.RespondAsync(t);
            if (response is not null)
            {
                transmissionContext.SendAndForget(response, this.DataId);
            }
        });

        return true;
    }
}

public interface INetResponder
{
    ulong DataId { get; }

    bool Respond(TransmissionContext transmissionContext);
}
