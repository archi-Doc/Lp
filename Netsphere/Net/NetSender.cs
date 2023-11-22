// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using Netsphere.Misc;

namespace Netsphere.Net;

internal class NetSender
{// LOG_NETSENDER
    public readonly struct Item
    {
        public Item(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared)
        {
            this.EndPoint = endPoint;
            this.MemoryOwner = toBeShared.IncrementAndShare();
        }

        public readonly IPEndPoint EndPoint;

        public readonly ByteArrayPool.MemoryOwner MemoryOwner;
    }

    public NetSender(NetTerminal netTerminal, ILogger<NetSender> logger)
    {
        this.UpdateSystemMics();
        this.netTerminal = netTerminal;
        this.logger = logger;
        this.netSocketIpv4 = new(this.netTerminal);
        this.netSocketIpv6 = new(this.netTerminal);
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

                this.sender.Initialize();

                this.sender.netTerminal.ProcessSend(this.sender);

                this.sender.Flush();

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

    public void Send_NotThreadSafe(IPEndPoint endPoint, ByteArrayPool.MemoryOwner toBeShared)
    {
#if LOG_NETSENDER
        this.logger.TryGet(LogLevel.Debug)?.Log($"{this.netTerminal.NetTerminalString} To {endPoint.ToString()}, {toBeShared.Span.Length} bytes");
#endif

        if (endPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            this.itemsIpv4.Enqueue(new(endPoint, toBeShared));
            /*if (this.netTerminal.netSocketIpv4.UnsafeUdpClient is { } client)
            {
                client.Send(data, endPoint);
            }*/
        }
        else
        {
            this.itemsIpv6.Enqueue(new(endPoint, toBeShared));
            /*if (this.netTerminal.netSocketIpv6.UnsafeUdpClient is { } client)
            {
                client.Send(data, endPoint);
            }*/
        }

#if LOG_NETSENDER
        this.logger.TryGet(LogLevel.Debug)?.Log($"{this.netTerminal.NetTerminalString} To {endPoint.ToString()}, {toBeShared.Span.Length} bytes done");
#endif
    }

    public long UpdateSystemMics()
        => this.currentSystemMics = Mics.GetSystem();

    public bool Start(ThreadCoreBase parent)
    {
        var port = this.netTerminal.Port
            ;
        if (!this.netSocketIpv4.Start(parent, port, false))
        {
            this.logger.TryGet(LogLevel.Fatal)?.Log($"Could not create a UDP socket with port {port}.");
            throw new PanicException();
        }

        if (!this.netSocketIpv6.Start(parent, port, true))
        {
            this.logger.TryGet(LogLevel.Fatal)?.Log($"Could not create a UDP socket with port {port}.");
            throw new PanicException();
        }

        this.sendCore ??= new SendCore(parent, this);
        this.sendCore.Start();
        return true;
    }

    public void Stop()
    {
        this.netSocketIpv4.Stop();
        this.netSocketIpv6.Stop();
        this.sendCore?.Dispose();
    }

    public bool CanSend => this.SendCapacity > this.SendCount;

    public long CurrentSystemMics => this.currentSystemMics;

    public int SendCapacity { get; private set; }

    public int SendCount { get; private set; }

    private readonly NetTerminal netTerminal;
    private readonly ILogger logger;
    private readonly NetSocket netSocketIpv4;
    private readonly NetSocket netSocketIpv6;
    private SendCore? sendCore;
    private long currentSystemMics;
    private Queue<Item> itemsIpv4 = new();
    private Queue<Item> itemsIpv6 = new();

    private void Initialize()
    {
        this.SendCapacity = 50;
        this.SendCount = 0;
    }

    private void Flush()
    {
        if (this.netSocketIpv4.UnsafeUdpClient is { } ipv4)
        {
            while (this.itemsIpv4.TryDequeue(out var item))
            {
                ipv4.Send(item.MemoryOwner.Span, item.EndPoint);
                item.MemoryOwner.Return();
            }
        }
        else
        {
            this.itemsIpv4.Clear();
        }

        if (this.netSocketIpv6.UnsafeUdpClient is { } ipv6)
        {
            while (this.itemsIpv6.TryDequeue(out var item))
            {
                ipv6.Send(item.MemoryOwner.Span, item.EndPoint);
                item.MemoryOwner.Return();
            }
        }
        else
        {
            this.itemsIpv6.Clear();
        }
    }
}
