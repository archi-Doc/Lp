// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Arc.Threading;

namespace LP.Net;

/// <summary>
/// NetSocket provides low-level network service.
/// </summary>
public class NetSocket
{
    private const int ReceiveTimeout = 100;
    private const int SendIntervalMilliseconds = 2;
    private const int SendIntervalNanoseconds = 2_000_000;

    internal class NetSocketRecvCore : ThreadCore
    {
        public static void Process(object? parameter)
        {
            var core = (NetSocketRecvCore)parameter!;
            while (true)
            {
                if (core.IsTerminated)
                {
                    break;
                }

                var udp = core.pipe.udpClient;
                if (udp == null)
                {
                    break;
                }

                try
                {
                    IPEndPoint remoteEP = default!;
                    var bytes = udp.Receive(ref remoteEP);
                    if (bytes.Length <= Netsphere.MaxPayload)
                    {
                        core.pipe.terminal.ProcessReceive(remoteEP, bytes, Ticks.GetSystem());
                    }

                    // var memory = new ReadOnlyMemory<byte>(bytes);
                    // while (!memory.IsEmpty)
                    {
                        /*var piece = TinyhandSerializer.Deserialize<IPiece>(memory, null, out var bytesRead);
                        core.NetSpherer.Receive(remoteEP, piece);
                        memory = memory.Slice(bytesRead);*/
                    }

                    /*IPEndPoint remoteEP = default!;
                    var bytes = this.udpClient.Receive(ref remoteEP);
                    var text = $"Received: {bytes.Length}";
                    if (bytes.Length >= sizeof(int))
                    {
                        text += $", First data: {BitConverter.ToInt32(bytes)}";
                    }

                    Log.Debug(text);*/
                }
                catch
                {
                }
            }
        }

        public NetSocketRecvCore(ThreadCoreBase parent, NetSocket pipe)
                : base(parent, Process, false)
        {
            this.pipe = pipe;
        }

        private NetSocket pipe;
    }

    internal class NetSocketSendCore : ThreadCore
    {
        public static void Process(object? parameter)
        {
            var core = (NetSocketSendCore)parameter!;
            while (true)
            {
                if (core.IsTerminated)
                {
                    break;
                }

                core.ProcessSend();

                // core.Sleep(SendIntervalMilliseconds);
                core.TryNanoSleep(SendIntervalNanoseconds); // Performs better than core.Sleep() on Linux.
            }
        }

        public NetSocketSendCore(ThreadCoreBase parent, NetSocket pipe)
                : base(parent, Process, false)
        {
            this.socket = pipe;
            this.timer = MultimediaTimer.TryCreate(SendIntervalMilliseconds, this.ProcessSend); // Use multimedia timer if available.
        }

        public void ProcessSend()
        {// Invoked by multiple threads.
            // Check interval.
            var currentTicks = Ticks.GetSystem();
            var previous = Volatile.Read(ref this.previousTicks);
            var interval = Ticks.FromNanoseconds((double)SendIntervalNanoseconds / 2); // Half for margin.
            if (currentTicks < (previous + interval))
            {
                return;
            }

            lock (this.socket.udpSync)
            {
                if (this.socket.udpClient != null)
                {
                    this.socket.terminal.ProcessSend(this.socket.udpClient, currentTicks);
                }
            }

            Volatile.Write(ref this.previousTicks, currentTicks);
        }

        protected override void Dispose(bool disposing)
        {
            this.timer?.Dispose();
            base.Dispose(disposing);
        }

        private NetSocket socket;
        private MultimediaTimer? timer;
        private long previousTicks;
    }

    public NetSocket(Terminal terminal)
    {
        this.terminal = terminal;
    }

    public bool TryStart(ThreadCoreBase parent, int port)
    {
        this.recvCore = new NetSocketRecvCore(parent, this);
        this.sendCore = new NetSocketSendCore(parent, this);

        try
        {
            this.PrepareUdpClient(port);
        }
        catch
        {
            Logger.Default.Error($"Could not create a UDP socket with port {port}.");
            return false;
        }

        this.recvCore.Start();
        this.sendCore.Start();

        return true;
    }

    public void Stop(Message.Stop message)
    {
        this.recvCore?.Dispose();
        this.sendCore?.Dispose();
        lock (this.udpSync)
        {
            this.udpClient?.Dispose();
            this.udpClient = null;
        }
    }

    private void PrepareUdpClient(int port)
    {
        var udp = new UdpClient(port);
        try
        {
            const int SIO_UDP_CONNRESET = -1744830452;
            udp.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
        }
        catch
        {
        }

        udp.Client.ReceiveTimeout = ReceiveTimeout;

        lock (this.udpSync)
        {
            if (this.udpClient != null)
            {
                this.udpClient.Dispose();
            }

            this.udpClient = udp;
        }
    }

    private Terminal terminal;
    private NetSocketRecvCore? recvCore;
    private NetSocketSendCore? sendCore;
    private object udpSync = new(); // sync object for UpdClient.
    private UdpClient? udpClient;

    private Stopwatch Stopwatch { get; } = new();
}
