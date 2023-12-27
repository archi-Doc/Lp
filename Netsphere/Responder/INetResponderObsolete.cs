// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;

namespace Netsphere.Responder;

public abstract class NetResponderObsolete<TSend, TReceive> : INetResponderObsolete
{
    public ulong GetDataId() => BlockService.GetId<TSend, TReceive>();

    public virtual TReceive? Respond(TSend value) => default;

    public bool Respond(ServerOperation operation, NetReceivedData received)
    {
        if (!TinyhandSerializer.TryDeserialize<TSend>(received.Received.Memory.Span, out var t))
        {
            var emptyTask = operation.SendEmpty();
            return false;
        }

        var response = this.Respond(t);
        if (response == null)
        {
            return false;
        }

        var task = operation.SendAsync(response);
        return true;
    }
}

public interface INetResponderObsolete
{
    public ulong GetDataId();

    public bool Respond(ServerOperation operation, NetReceivedData received);
}
