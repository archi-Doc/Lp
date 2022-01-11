// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Arc.Threading;

namespace Netsphere;

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

            IPEndPoint anyEP;
            if (core.socket.udpClient?.Client.AddressFamily == AddressFamily.InterNetwork)
            {
                anyEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            }
            else
            {
                anyEP = new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort);
            }

            ByteArrayPool.Owner? arrayOwner = null;
            while (true)
            {
                if (core.IsTerminated)
                {
                    break;
                }

                var udp = core.socket.udpClient;
                if (udp == null)
                {
                    break;
                }

                try
                {
                    // IPEndPoint remoteEP = default!;
                    // var bytes = udp.Receive(ref remoteEP);
                    var remoteEP = (EndPoint)anyEP;
                    arrayOwner ??= PacketPool.Rent();
                    var received = udp.Client.ReceiveFrom(arrayOwner.ByteArray, 0, arrayOwner.ByteArray.Length, SocketFlags.None, ref remoteEP);
                    core.socket.terminal.TerminalLogger?.Information(received.ToString()); // temporary
                    if (received <= NetControl.MaxPayload)
                    {
                        core.socket.terminal.ProcessReceive((IPEndPoint)remoteEP, arrayOwner, received, Mics.GetSystem());
                        if (arrayOwner.Count > 1)
                        {// Byte array is used by multiple owners. Return and rent a new one next time.
                            arrayOwner = arrayOwner.Return();
                        }
                    }
                }
                catch
                {
                }
            }
        }

        public NetSocketRecvCore(ThreadCoreBase parent, NetSocket socket)
                : base(parent, Process, false)
        {
            this.socket = socket;
        }

        private NetSocket socket;
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

        public NetSocketSendCore(ThreadCoreBase parent, NetSocket socket)
                : base(parent, Process, false)
        {
            this.socket = socket;
            this.timer = MultimediaTimer.TryCreate(SendIntervalMilliseconds, this.ProcessSend); // Use multimedia timer if available.
        }

        public void ProcessSend()
        {// Invoked by multiple threads.
            // Check interval.
            var currentMics = Mics.GetSystem();
            var previous = Volatile.Read(ref this.previousMics);
            var interval = Mics.FromNanoseconds((double)SendIntervalNanoseconds / 2); // Half for margin.
            if (currentMics < (previous + interval))
            {
                return;
            }

            lock (this.socket.udpSync)
            {
                if (this.socket.udpClient != null)
                {
                    this.socket.terminal.ProcessSend(this.socket.udpClient, currentMics);
                }
            }

            Volatile.Write(ref this.previousMics, currentMics);
        }

        protected override void Dispose(bool disposing)
        {
            this.timer?.Dispose();
            base.Dispose(disposing);
        }

        private NetSocket socket;
        private MultimediaTimer? timer;
        private long previousMics;
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
