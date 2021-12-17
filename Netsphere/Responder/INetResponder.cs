// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netsphere.Responder;

public abstract class NetResponder<TSend, TReceive> : INetResponder
{
    public ulong GetDataId() => BlockService.GetId<TSend, TReceive>();

    public virtual TReceive? Respond(TSend value) => default;

    public bool Respond(ServerTerminal terminal, NetInterfaceReceivedData received)
    {
        if (!TinyhandSerializer.TryDeserialize<TSend>(received.Received.Memory, out var t))
        {
            return false;
        }

        var response = this.Respond(t);
        if (response == null)
        {
            return false;
        }

        var task = terminal.SendAsync(response);
        return true;
    }
}

public interface INetResponder
{
    public ulong GetDataId();

    public bool Respond(ServerTerminal terminal, NetInterfaceReceivedData received);
}
