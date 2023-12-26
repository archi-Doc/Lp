// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Packet;

namespace Netsphere.Server;

public class ConnectionContext
{
    public ConnectionContext(IServiceProvider serviceProvider, ServerConnection serverConnection)
    {
        this.ServiceProvider = serviceProvider;
        this.ServerConnection = serverConnection;
    }

    public void InvokeSync(TransmissionContext transmissionContext)
    {// transmissionContext.Return();
        if (transmissionContext.DataKind == 0)
        {// Block (Responder)
            transmissionContext.SendAndForget(new PacketPingResponse(NetAddress.Alternative, "Alternativ"));
        }
        else if (transmissionContext.DataKind == 1)
        {// RPC
        }
    }

    public IServiceProvider ServiceProvider { get; internal set; } = default!;

    public ServerConnection ServerConnection { get; internal set; } = default!;
}
