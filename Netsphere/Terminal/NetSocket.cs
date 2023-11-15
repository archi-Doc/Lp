// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using Netsphere.Misc;

namespace Netsphere;

/// <summary>
/// NetSocket provides low-level network service.
/// </summary>
public class NetSocket
{
    private const int ReceiveTimeout = 100;

    internal class NetSocketRecvCore : ThreadCore
    {
        public static void Process(object? parameter)
        {
            var core = (NetSocketRecvCore)parameter!;

            IPEndPoint anyEP;
            if (core.socket.UnsafeUdpClient?.Client.AddressFamily == AddressFamily.InterNetwork)
            {
                anyEP = new IPEndPoint(IPAddress.Any, 0); // IPEndPoint.MinPort
            }
            else
            {
                anyEP = new IPEndPoint(IPAddress.IPv6Any, 0); // IPEndPoint.MinPort
            }

            ByteArrayPool.Owner? arrayOwner = null;
            while (!core.IsTerminated)
            {
                var udp = core.socket.UnsafeUdpClient;
                if (udp == null)
                {
                    break;
                }

                try
                {// nspi 10^5
                    var remoteEP = (EndPoint)anyEP;
                    arrayOwner ??= PacketPool.Rent();
                    var received = udp.Client.ReceiveFrom(arrayOwner.ByteArray, 0, arrayOwner.ByteArray.Length, SocketFlags.None, ref remoteEP);
                    // ValueTask<SocketReceiveFromResult> vt = udp.Client.ReceiveFromAsync(arrayOwner.ByteArray.AsMemory(), SocketFlags.None, remoteEP);
                    if (received <= NetControl.MaxPayload)
                    {// nspi
                        // var systemMics = Mics.GetSystem();
                        var currentMics = core.socket.CurrentSystemMics;
                        core.socket.terminal.ProcessReceive((IPEndPoint)remoteEP, arrayOwner, received, currentMics);
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
            while (!core.IsTerminated)
            {
                var prev = Mics.GetSystem();
                core.ProcessSend();

                var nano = NetConstants.SendIntervalNanoseconds - ((Mics.GetSystem() - prev) * 1000);
                if (nano > 0)
                {
                    core.socket.Logger?.TryGet()?.Log($"Nanosleep: {nano}");
                    core.TryNanoSleep(nano); // Performs better than core.Sleep() on Linux.
                }
            }
        }

        public NetSocketSendCore(ThreadCoreBase parent, NetSocket socket)
                : base(parent, Process, false)
        {
            this.socket = socket;
            this.timer = MultimediaTimer.TryCreate(NetConstants.SendIntervalMilliseconds, this.ProcessSend); // Use multimedia timer if available.
        }

        public void ProcessSend()
        {// Invoked by multiple threads(NetSocketSendCore.Process() or MultimediaTimer).
            lock (this.syncObject)
            {
                // Check interval.
                var currentMics = this.socket.UpdateSystemMics();
                var interval = Mics.FromNanoseconds((double)NetConstants.SendIntervalNanoseconds / 2); // Half for margin.
                if (currentMics < (this.previousMics + interval))
                {
                    return;
                }

                if (this.socket.UnsafeUdpClient is not null)
                {
                    // this.socket.Logger?.TryGet()?.Log($"ProcessSend");
                    this.socket.terminal.ProcessSend(currentMics);
                }

                this.previousMics = currentMics;
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.timer?.Dispose();
            base.Dispose(disposing);
        }

        private NetSocket socket;
        private MultimediaTimer? timer;

        private object syncObject = new();
        private long previousMics;
    }

    public NetSocket(Terminal terminal)
    {
        this.terminal = terminal;
        this.logger = terminal.UnitLogger.GetLogger<NetSocket>();
        this.UpdateSystemMics();
    }

    #region FieldAndProperty

    public long CurrentSystemMics => this.currentSystemMics;

    internal ILogger? Logger => this.terminal.IsAlternative ? null : this.logger;

#pragma warning disable SA1401 // Fields should be private
    internal UdpClient? UnsafeUdpClient;
#pragma warning restore SA1401 // Fields should be private

    private Terminal terminal;
    private ILogger logger;
    private NetSocketRecvCore? recvCore;
    private NetSocketSendCore? sendCore;
    private long currentSystemMics;

    private Stopwatch Stopwatch { get; } = new();

    #endregion

    public long UpdateSystemMics()
        => this.currentSystemMics = Mics.GetSystem();

    public bool Start(ThreadCoreBase parent, int port, bool ipv6)
    {
        this.recvCore = new NetSocketRecvCore(parent, this);
        this.sendCore = new NetSocketSendCore(parent, this);

        try
        {
            this.PrepareUdpClient(port, ipv6);
        }
        catch
        {
            this.Logger?.TryGet(LogLevel.Fatal)?.Log($"Could not create a UDP socket with port {port}.");
            throw new PanicException();
        }

        this.recvCore.Start();
        this.sendCore.Start();

        return true;
    }

    public void Stop()
    {
        this.recvCore?.Dispose();
        this.sendCore?.Dispose();

        try
        {
            if (this.UnsafeUdpClient != null)
            {
                this.UnsafeUdpClient.Dispose();
                this.UnsafeUdpClient = null;
            }
        }
        catch
        {
        }
    }

    private void PrepareUdpClient(int port, bool ipv6)
    {
        var addressFamily = ipv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
        var udp = new UdpClient(port, addressFamily);
        try
        {
            const int SIO_UDP_CONNRESET = -1744830452;
            udp.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
        }
        catch
        {
        }

        udp.Client.ReceiveTimeout = ReceiveTimeout;

        try
        {
            if (this.UnsafeUdpClient != null)
            {
                this.UnsafeUdpClient.Dispose();
                this.UnsafeUdpClient = null;
            }
        }
        catch
        {
        }

        this.UnsafeUdpClient = udp;
    }
}
