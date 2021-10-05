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

    internal class PipeReadCore : ThreadCore
    {
        public static void Process(object? parameter)
        {
            var core = (PipeReadCore)parameter!;
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

        public PipeReadCore(ThreadCoreBase parent, Pipe pipe)
                : base(parent, Process, false)
        {
            this.pipe = pipe;
        }

        private Pipe pipe;
    }

    public Pipe(Information information)
    {
        this.information = information;

        Radio.Open<Message.Start>(this.Start);
    }

    public void Start(Message.Start message)
    {
        this.readCore = new PipeReadCore(message.ParentCore, this);
        this.readCore.Thread.Priority = ThreadPriority.AboveNormal;

        this.udpClient = new UdpClient(this.information.ConsoleOptions.NetsphereOptions.Port);
        try
        {
            const int SIO_UDP_CONNRESET = -1744830452;
            this.udpClient.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
        }
        catch
        {
        }

        this.udpClient.Client.ReceiveTimeout = ReceiveTimeout;
        this.readCore.Start();
    }

    private Information information;
    private PipeReadCore? readCore;
    private UdpClient? udpClient;

    private Stopwatch Stopwatch { get; } = new();
}
