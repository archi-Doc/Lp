// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace Netsphere;

public class Server
{
    public Server(NetBase netBase, NetControl netControl, NetService netService)
    {
        this.NetBase = netBase;
        this.NetControl = netControl;
        this.NetService = netService;

        this.ServerContext = this.NetControl.NewServerContext();
        this.ServerContext.ServiceProvider = this.NetControl.ServiceProvider;
        this.NetService.ServerContext = this.ServerContext;
        this.NetService.NewCallContext = this.NetControl.NewCallContext;
    }

    public async Task Process(ServerTerminal terminal)
    {
        this.NetTerminal = terminal;
        this.NetTerminal.SetMaximumResponseTime(1000);

        while (!this.NetTerminal.IsClosed)
        {
            (var operation, var received) = await terminal.ReceiveAsync().ConfigureAwait(false);
            try
            {
                if (received.Result == NetResult.Success)
                {// Success
                    if (received.PacketId == PacketId.Data &&
                        this.NetControl.Responders.TryGetValue(received.DataId, out var responder) &&
                        responder.Respond(operation!, received))
                    {// Responder
                        continue;
                    }
                    else if (received.PacketId == PacketId.Rpc)
                    {// RPC
                        var op = operation!;
                        operation = null;
                        received.Received.IncrementAndShare();
                        var task = this.NetService.Process(op, received); // .ConfigureAwait(false);
                        continue;
                    }

                    // Essential (PacketPunch)
                    if (this.ProcessEssential(operation!, received))
                    {
                        continue;
                    }

                    continue;
                }
                else if (received.Result == NetResult.Timeout ||
                    received.Result == NetResult.NoReceiver)
                {
                    this.NetTerminal.SendClose();
                    break;
                }
                else if (received.Result == NetResult.Closed)
                {
                    break;
                }
            }
            finally
            {
                operation?.Dispose();
                received.Return();
                // terminal.ClearSender();
            }
        }

        terminal.TerminalLogger?.Information($"Server offline.");
    }

    public ThreadCoreBase? Core => this.NetControl.Terminal.Core;

    public NetBase NetBase { get; }

    public NetControl NetControl { get; }

    public NetService NetService { get; }

    public ServerTerminal NetTerminal { get; private set; } = default!;

    public ServerContext ServerContext { get; }

    private bool ProcessEssential(ServerOperation operation, NetReceivedData received)
    {
        if (received.PacketId == PacketId.Punch)
        {
            return this.ProcessEssential_Punch(operation, received);
        }

        if (this.NetBase.NetsphereOptions.EnableTestFeatures)
        {
            if (received.PacketId == PacketId.Test)
            {
                return this.ProcessEssential_Test(operation, received);
            }
        }

        return false;
    }

    private bool ProcessEssential_Punch(ServerOperation operation, NetReceivedData received)
    {
        if (!TinyhandSerializer.TryDeserialize<PacketPunch>(received.Received.Memory, out var punch))
        {
            return false;
        }

        TimeCorrection.AddCorrection(punch.UtcMics);

        var response = new PacketPunchResponse();
        response.Endpoint = this.NetTerminal.Endpoint;
        response.UtcMics = Mics.GetUtcNow();

        var task = operation.SendPacketAsync(response);
        return true;
    }

    private bool ProcessEssential_Test(ServerOperation operation, NetReceivedData received)
    {
        if (!TinyhandSerializer.TryDeserialize<TestPacket>(received.Received.Memory, out var r))
        {
            var task2 = operation.SendEmpty();
            return false;
        }

        var response = TestPacket.Create(2000);
        var task = operation.SendAsync(response);
        return true;
    }
}
