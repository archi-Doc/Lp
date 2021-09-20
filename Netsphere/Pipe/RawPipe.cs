// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Arc.Threading;

namespace LP.Net;

public class RawPipe
{
    public RawPipe(ThreadCoreBase core)
        : this(core, (int)(Constants.MinPort + ((Constants.MaxPort - Constants.MinPort + 1) * Random.Shared.NextDouble())))
    {
    }

    public RawPipe(ThreadCoreBase core, int port)
    {
        this.core = new ThreadCore(ThreadCore.Root, this.Process, false);
        this.core.Thread.Priority = ThreadPriority.AboveNormal;

        this.udpPort = new UdpClient(port);
        try
        {
            const int SIO_UDP_CONNRESET = -1744830452;
            this.udpPort.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
        }
        catch
        {
        }

        this.core.Start();
    }

    private void Process(object? param)
    {
        var core = (ThreadCore)param!;
        while (true)
        {
            if (core.IsTerminated)
            {
                break;
            }
            else if (this.udpPort.Available == 0)
            {
                this.Stopwatch.Start();
                while (this.Stopwatch.Elapsed < TimeSpan.FromMilliseconds(1))
                {
                    if (core.IsTerminated)
                    {
                        break;
                    }

                    Thread.Sleep(0); // Thread.Sleep(1) is actually not 1 millisecond.
                }

                continue;
            }

            IPEndPoint remoteEP = default!;
            var bytes = this.udpPort.Receive(ref remoteEP);
            var text = $"Received: {bytes.Length}";
            if (bytes.Length >= sizeof(int))
            {
                text += $", First data: {BitConverter.ToInt32(bytes)}";
            }

            // Log.Debug(text);
            // Log.Debug($"Sender address: {remoteEP.Address}, port: {remoteEP.Port}");
        }
    }

    private ThreadCore core;
    private UdpClient udpPort;

    private Stopwatch Stopwatch { get; } = new();
}
