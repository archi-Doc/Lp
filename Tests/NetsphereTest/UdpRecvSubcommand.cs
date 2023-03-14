// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using System.Net.Sockets;
using Arc.Unit;
using SimpleCommandLine;

namespace NetsphereTest;

[SimpleCommand("udprecv")]
public class UdpRecvSubcommand : ISimpleCommandAsync<UdpRecvOptions>
{
    public UdpRecvSubcommand(ILogger<UdpRecvSubcommand> logger)
    {
        this.logger = logger;
    }

    public async Task RunAsync(UdpRecvOptions options, string[] args)
    {
        Console.WriteLine($"udprecv: {options.Port.ToString()}");

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

        var buffer = new byte[2048];
        while (!ThreadCore.Root.IsTerminated)
        {
            var anyEP = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            var remoteEP = (EndPoint)anyEP;
            try
            {
                var received = udp.Client.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEP);

                udp.Send(buffer, (IPEndPoint)remoteEP);
            }
            catch
            {
            }
        }

        udp.Dispose();

        Console.WriteLine("Exit");
    }

    private ILogger logger;
}

public record UdpRecvOptions
{
    [SimpleOption("port", Description = "Port number associated with the address")]
    public int Port { get; set; } = 1986;

    // [SimpleOption("node", Description = "Node address", Required = true)]
    // public string Node { get; init; } = string.Empty;
}
