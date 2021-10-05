// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Arc.Threading;

namespace LP.Net;

/// <summary>
/// Pipe provides low-level network service.
/// </summary>
public class Pipe
{
    private const int ReceiveTimeout = 100;

    internal class PipeRecvCore : ThreadCore
    {
        public static void Process(object? parameter)
        {
            var core = (PipeRecvCore)parameter!;
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

                    var memory = new ReadOnlyMemory<byte>(bytes);
                    while (!memory.IsEmpty)
                    {
                        /*var piece = TinyhandSerializer.Deserialize<IPiece>(memory, null, out var bytesRead);
                        core.NetSpherer.Receive(remoteEP, piece);
                        memory = memory.Slice(bytesRead);*/
                        memory = memory.Slice(1);
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

        public PipeRecvCore(ThreadCoreBase parent, Pipe pipe)
                : base(parent, Process, false)
        {
            this.pipe = pipe;
        }

        private Pipe pipe;
    }

    internal class PipeSendCore : ThreadCore
    {
        public static void Process(object? parameter)
        {
            var core = (PipeSendCore)parameter!;
            while (true)
            {
                if (core.IsTerminated)
                {
                    break;
                }

                core.ProcessSend();
                core.Sleep(1);
            }
        }

        public PipeSendCore(ThreadCoreBase parent, Pipe pipe)
                : base(parent, Process, false)
        {
            this.pipe = pipe;
            this.timer = MultimediaTimer.TryCreate(1, this.ProcessSend);
        }

        public void ProcessSend()
        {
            var udp = this.pipe.udpClient;
            if (udp == null)
            {
                return;
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.timer?.Dispose();
            base.Dispose(disposing);
        }

        private Pipe pipe;
        private MultimediaTimer? timer;
    }

    public Pipe(Information information)
    {
        this.information = information;

        Radio.Open<Message.Start>(this.Start);
        Radio.Open<Message.Stop>(this.Stop);
    }

    public void Start(Message.Start message)
    {
        this.recvCore = new PipeRecvCore(message.ParentCore, this);
        this.recvCore.Thread.Priority = ThreadPriority.AboveNormal;
        this.sendCore = new PipeSendCore(message.ParentCore, this);
        this.sendCore.Thread.Priority = ThreadPriority.AboveNormal;

        this.PrepareUdpClient(this.information.ConsoleOptions.NetsphereOptions.Port);

        this.recvCore.Start();
    }

    public void Stop(Message.Stop message)
    {
        this.recvCore?.Dispose();
        this.sendCore?.Dispose();
        this.udpClient?.Dispose();
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

        var prev = Interlocked.Exchange(ref this.udpClient, udp);
        if (prev != null)
        {
            prev.Dispose();
        }
    }

    private Information information;
    private PipeRecvCore? recvCore;
    private PipeSendCore? sendCore;
    private UdpClient? udpClient;

    private Stopwatch Stopwatch { get; } = new();
}
