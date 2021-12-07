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

    public async Task Process(NetTerminalServer terminal)
    {
        this.NetTerminal = terminal;
        while (true)
        {
            terminal.ClearSender();
            var received = await terminal.ReceiveAsync();
            if (received.Result == NetInterfaceResult.Success && received.Packet is { } packet)
            {
                if (this.ProcessEssential(packet))
                {
                    continue;
                }
            }
            else if (received.Result == NetInterfaceResult.Timeout ||
                received.Result == NetInterfaceResult.Closed)
            {
                break;
            }
        }
    }

    public ThreadCoreBase? Core => this.NetControl.Terminal.Core;

    public NetBase NetBase { get; }

    public NetControl NetControl { get; }

    public NetTerminalServer NetTerminal { get; private set; } = default!;

    private bool ProcessEssential(NetTerminalServerPacket packet)
    {
        if (packet.PacketId == PacketId.Punch)
        {
            return this.ProcessEssential_Punch(packet);
        }

        return false;
    }

    private bool ProcessEssential_Punch(NetTerminalServerPacket packet)
    {
        if (!TinyhandSerializer.TryDeserialize<PacketPunch>(packet.Data, out var punch))
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
