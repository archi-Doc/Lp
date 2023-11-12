// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class Server
{
    public Server(LPBase lpBase, NetBase netBase, NetControl netControl)
    {// InvokeServer()
        this.LpBase = lpBase;
        this.NetBase = netBase;
        this.NetControl = netControl;
        this.NetService = new NetService(this.NetControl.ServiceProvider);

        this.ServerContext = this.NetControl.NewServerContext();
        this.ServerContext.ServiceProvider = this.NetControl.ServiceProvider;
        this.NetService.ServerContext = this.ServerContext;
        this.NetService.NewCallContext = this.NetControl.NewCallContext;
    }

    public async Task Process(ServerTerminal terminal)
    {
        this.NetTerminal = terminal;
        this.NetTerminal.SetMaximumResponseTime(1000);
        this.ServerContext.Terminal = terminal;

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
                    this.NetTerminal.Logger?.Log($"{received.Result} -> SendClose()");
                    this.NetTerminal.SendClose();
                    break;
                }
                else if (received.Result == NetResult.Closed)
                {
                    this.NetTerminal.Logger?.Log($"{received.Result}");
                    break;
                }
            }
            finally
            {
                // operation?.Dispose(); // Don't dispose (net operation holds data waiting to be sent)
                received.Return();
            }
        }

        this.NetTerminal.Logger?.Log($"Server offline.");
    }

    public ThreadCoreBase? Core => this.NetControl.Terminal.Core;

    public LPBase LpBase { get; }

    public NetBase NetBase { get; }

    public NetControl NetControl { get; }

    public NetService NetService { get; }

    public ServerTerminal NetTerminal { get; private set; } = default!;

    public ServerContext ServerContext { get; private set; }

    private bool ProcessEssential(ServerOperation operation, NetReceivedData received)
    {
        if (received.PacketId == PacketId.Punch)
        {
            return this.ProcessEssential_Punch(operation, received);
        }

        if (this.LpBase.TestFeatures)
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
        if (!TinyhandSerializer.TryDeserialize<PacketPunch>(received.Received.Memory.Span, out var punch))
        {
            return false;
        }

        this.NetTerminal.Logger?.Log("Respond: PacketPunch");

        TimeCorrection.AddCorrection(punch.UtcMics);

        var response = new PacketPunchResponse();
        response.Endpoint = this.NetTerminal.Endpoint.EndPoint;
        response.UtcMics = Mics.GetUtcNow();

        var task = operation.SendPacketAsync(response);
        return true;
    }

    private bool ProcessEssential_Test(ServerOperation operation, NetReceivedData received)
    {
        if (!TinyhandSerializer.TryDeserialize<TestPacket>(received.Received.Memory.Span, out var r))
        {
            var task2 = operation.SendEmpty();
            return false;
        }

        this.NetTerminal.Logger?.Log("Respond: TestPacket");

        var response = TestPacket.Create(2000);
        var task = operation.SendAsync(response);
        return true;
    }
}
