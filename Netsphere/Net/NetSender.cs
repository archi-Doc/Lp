// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Misc;
using static Netsphere.Net.NetSocket;

namespace Netsphere.Net;

internal class NetSender
{
    public NetSender(NetTerminal netTerminal)
    {
        this.UpdateSystemMics();
        this.netTerminal = netTerminal;
    }

    private class SendCore : ThreadCore
    {
        public static void Process(object? parameter)
        {
            var core = (SendCore)parameter!;
            while (!core.IsTerminated)
            {
                var prev = Mics.GetSystem();
                core.ProcessSend();

                var nano = NetConstants.SendIntervalNanoseconds - ((Mics.GetSystem() - prev) * 1000);
                if (nano > 0)
                {
                    // core.socket.Logger?.TryGet()?.Log($"Nanosleep: {nano}");
                    core.TryNanoSleep(nano); // Performs better than core.Sleep() on Linux.
                }
            }
        }

        public SendCore(ThreadCoreBase parent, NetSender sender)
                : base(parent, Process, false)
        {
            this.sender = sender;
            this.timer = MultimediaTimer.TryCreate(NetConstants.SendIntervalMilliseconds, this.ProcessSend); // Use multimedia timer if available.
        }

        public void ProcessSend()
        {// Invoked by multiple threads(NetSocketSendCore.Process() or MultimediaTimer).
            lock (this.syncObject)
            {
                // Check interval.
                var currentSystemMics = this.sender.UpdateSystemMics();
                var interval = Mics.FromNanoseconds((double)NetConstants.SendIntervalNanoseconds / 2); // Half for margin.
                if (currentSystemMics < (this.previousSystemMics + interval))
                {
                    return;
                }

                this.sender.netTerminal.ProcessSend(this.sender);

                this.previousSystemMics = currentSystemMics;
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.timer?.Dispose();
            base.Dispose(disposing);
        }

        private NetSender sender;
        private MultimediaTimer? timer;

        private object syncObject = new();
        private long previousSystemMics;
    }

    public void Send(IPEndPoint endPoint, Span<byte> data)
    {
        if (endPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            if (this.netTerminal.netSocketIpv4.UnsafeUdpClient is { } client)
            {
                client.Send(data, endPoint);
            }
        }
        else
        {
            if (this.netTerminal.netSocketIpv6.UnsafeUdpClient is { } client)
            {
                client.Send(data, endPoint);
            }
        }
    }

    public long UpdateSystemMics()
        => this.currentSystemMics = Mics.GetSystem();

    public bool Start(ThreadCoreBase parent)
    {
        this.sendCore ??= new SendCore(parent, this);
        this.sendCore.Start();
        return true;
    }

    public void Stop()
    {
        this.sendCore?.Dispose();
    }

    public long CurrentSystemMics => this.currentSystemMics;

    private readonly NetTerminal netTerminal;
    private SendCore? sendCore;
    private long currentSystemMics;
}
