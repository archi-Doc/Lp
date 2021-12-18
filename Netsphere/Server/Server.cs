// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace Netsphere;

public class Server
{
    public const int DefaultMillisecondsToWait = 3000;

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
                var received = await terminal.ReceiveAsync(DefaultMillisecondsToWait);
                if (received.Result == NetInterfaceResult.Success)
                {// Success
                    // Responder (DataId, RPC)
                    if (this.NetControl.Respondes.TryGetValue(received.DataId, out var responder) &&
                        responder.Respond(terminal, received))
                    {
                        continue;
                    }

                    // Essential (PacketPunch)
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

        if (this.NetBase.NetsphereOptions.EnableTest)
        {
            if (received.PacketId == PacketId.Test)
            {
                return this.ProcessEssential_Test(received);
            }
        }

        return false;
    }

    private bool ProcessEssential_Punch(NetInterfaceReceivedData received)
    {
        if (!TinyhandSerializer.TryDeserialize<PacketPunch>(received.Received.Memory, out var punch))
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

    private bool ProcessEssential_Test(NetInterfaceReceivedData received)
    {
        if (!TinyhandSerializer.TryDeserialize<TestPacket>(received.Received.Memory, out var r))
        {
            return false;
        }

        var response = TestPacket.Create(2000);
        var task = this.NetTerminal.SendAsync(response);
        return true;
    }
}
