// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Arc.Crypto;
using Arc.Unit;
using SimpleCommandLine;

namespace NetsphereTest;

[SimpleCommand("udpsend")]
public class UdpSendSubcommand : ISimpleCommandAsync<UdpSendOptions>
{
    public UdpSendSubcommand(ILogger<UdpSendSubcommand> logger)
    {
        this.logger = logger;
    }

    public async Task RunAsync(UdpSendOptions options, string[] args)
    {
        if (!NodeAddress.TryParse(options.Node, out var node))
        {
            return;
        }

        Console.WriteLine($"udpsend: {node.ToString()}");

        var udp = new UdpClient(options.Port);
        try
        {
            const int SIO_UDP_CONNRESET = -1744830452;
            udp.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
        }
        catch
        {
        }

        udp.Client.ReceiveTimeout = 100;
        // udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, 128 * 1024);
        // udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, 128 * 1024);

        this.options = options;
        if (this.options.Size > 1400)
        {
            this.options.Size = 1400;
        }

        if (this.options.Repeat < 1)
        {
            this.options.Repeat = 1;
        }

        this.data = new byte[options.Size];
        RandomVault.Pseudo.NextBytes(this.data);
        this.udp = udp;
        this.endpoint = node.CreateEndpoint();

        await this.Process();

        udp.Dispose();

        Console.WriteLine("Exit");
    }

    private async Task Process()
    {
        for (var n = options.Start; n <= options.End; n += 10)
        {
            for (var r = 0; r < options.Repeat; r++)
            {
                if (ThreadCore.Root.IsTerminated)
                {
                    return;
                }

                Console.WriteLine($"Number: {n}, Size: {options.Size} bytes");
                var result = await SendAndRecv(n);

                if (!result)
                {
                    Console.WriteLine("Abort");
                    return;
                }
            }
        }
    }

    private async Task<bool> SendAndRecv(int number)
    {
        // Send
        for (var n = 0; n < number; n++)
        {
            udp.Send(data, this.endpoint);
        }

        // Recv
        var buffer = new byte[2048];
        var count = 0;
        var sw = Stopwatch.StartNew();
        while (!ThreadCore.Root.IsTerminated)
        {
            var anyEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            var remoteEP = (EndPoint)anyEP;
            try
            {
                var spinner = new SpinWait();
                while (spinner.Count < 140)
                {
                    spinner.SpinOnce(sleep1Threshold: -1);
                }

                var received = udp.Client.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEP);
                count++;

                if (count >= number)
                {
                    break;
                }
            }
            catch
            {
            }

            if (sw.ElapsedMilliseconds > 1000)
            {
                break;
            }
        }

        Console.WriteLine($"{count}/{number}");
        return count > (number >> 1);
    }

    private ILogger logger;
    UdpSendOptions options = default!;
    private byte[] data = default!;
    private UdpClient udp = default!;
    private IPEndPoint endpoint = default!;
}

public record UdpSendOptions
{
    [SimpleOption("node", Description = "Node address", Required = true)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("port", Description = "Port number associated with the address")]
    public int Port { get; set; } = 1981;

    [SimpleOption("size")]
    public int Size { get; set; } = 32;

    [SimpleOption("start")]
    public int Start { get; set; } = 10;

    [SimpleOption("end")]
    public int End { get; set; } = 100;

    [SimpleOption("repeat")]
    public int Repeat { get; set; } = 1;

    public override string ToString() => $"{this.Node}";
}
