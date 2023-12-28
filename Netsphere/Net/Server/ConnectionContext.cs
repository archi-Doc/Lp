// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Server;

public class ConnectionContext
{
    public ConnectionContext(IServiceProvider serviceProvider, ServerConnection serverConnection)
    {
        this.ServiceProvider = serviceProvider;
        this.NetTerminal = serverConnection.ConnectionTerminal.NetTerminal;
        this.ServerConnection = serverConnection;
    }

    public virtual bool InvokeCustom(TransmissionContext transmissionContext)
    {
        return false;
    }

    public void InvokeSync(TransmissionContext transmissionContext)
    {// transmissionContext.Return();
        if (transmissionContext.DataKind == 0)
        {// Block (Responder)
            if (this.NetTerminal.NetControl.TryGetResponder(transmissionContext.DataId, out var responder))
            {
                responder.Respond(transmissionContext);
            }
            else
            {
                transmissionContext.Return();
                return;
            }
        }
        else if (transmissionContext.DataKind == 1)
        {// RPC
        }

        if (!this.InvokeCustom(transmissionContext))
        {
            transmissionContext.Return();
        }
    }

    public IServiceProvider ServiceProvider { get; }

    public NetTerminal NetTerminal { get; }

    public ServerConnection ServerConnection { get; }
}
