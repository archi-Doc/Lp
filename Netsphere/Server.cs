// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace Netsphere;

public class Server
{
    public Server(NetBase netBase, NetControl netControl)
    {
        this.NetBase = netBase;
        this.NetControl = netControl;
    }

    public async Task Process(ServerTerminal terminal)
    {
        this.NetTerminal = terminal;
        while (!this.NetTerminal.IsClosed)
        {
            try
            {
                var received = await terminal.ReceiveAsync();
                if (received.Result == NetInterfaceResult.Success)
                {// Success
                    if (this.ProcessEssential(received))
                    {
                        continue;
                    }
                }
                else if (received.Result == NetInterfaceResult.Timeout ||
                    received.Result == NetInterfaceResult.Closed ||
                    received.Result == NetInterfaceResult.NoReceiver)
                {
                    break;
                }
            }
            finally
            {
                terminal.ClearSender();
            }
        }

        terminal.TerminalLogger?.Information($"Server offline.");
    }

    public ThreadCoreBase? Core => this.NetControl.Terminal.Core;

    public NetBase NetBase { get; }

    public NetControl NetControl { get; }

    public ServerTerminal NetTerminal { get; private set; } = default!;

    private bool ProcessEssential(NetInterfaceReceivedData received)
    {
        if (received.PacketId == PacketId.Punch)
        {
            return this.ProcessEssential_Punch(received);
        }
        else if (received.DataId == BlockService.GetId<TestBlock, TestBlock>())
        {
            if (!TinyhandSerializer.TryDeserialize<TestBlock>(received.Received, out var t))
            {
                return false;
            }

            var task = this.NetTerminal.SendAsync(t);
        }

        return false;
    }

    private bool ProcessEssential_Punch(NetInterfaceReceivedData received)
    {
        if (!TinyhandSerializer.TryDeserialize<PacketPunch>(received.Received, out var punch))
        {
            return false;
        }

        TimeCorrection.AddCorrection(punch.UtcTicks);

        var response = new PacketPunchResponse();
        response.Endpoint = this.NetTerminal.Endpoint;
        response.UtcTicks = Ticks.GetUtcNow();

        var task = this.NetTerminal.SendPacketAsync(response);
        return true;
    }
}
