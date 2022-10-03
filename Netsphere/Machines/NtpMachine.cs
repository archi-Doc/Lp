// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net.Sockets;
using LP.Machines.Ntp;
using Netsphere;

namespace LP.Machines;

[MachineObject(0x5e1f81ca, Group = typeof(SingleGroup<>))]
public partial class NtpMachine : Machine<Identifier>
{
    public NtpMachine(ILogger<NtpMachine> logger, BigMachine<Identifier> bigMachine, LPBase lpBase, NetBase netBase, NetControl netControl)
        : base(bigMachine)
    {
        this.logger = logger;
        this.NetBase = netBase;
        this.NetControl = netControl;
        this.LPBase = lpBase;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    public LPBase LPBase { get; }

    public NetBase NetBase { get; }

    public NetControl NetControl { get; }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        this.logger.TryGet(LogLevel.Information)?.Log($"{nameof(NtpMachine)} {this.count}");
        this.count++;

        var dateTime = await this.GetTime();
        this.logger.TryGet(LogLevel.Information)?.Log($"{dateTime.ToString()}");

        return StateResult.Continue;
    }

    private async Task<DateTime> GetTime()
    {
        const string ntpServer = "time.google.com";

        var data = new byte[48];
        data[0] = 0x1B;

        /*var addresses = Dns.GetHostEntry(ntpServer).AddressList;
        var endpoint = new IPEndPoint(addresses[0], 123);
        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            socket.Connect(endpoint);
            socket.ReceiveTimeout = 3000;
            socket.Send(data);
            socket.Receive(data);
            socket.Close();
        }*/

        using (var client = new UdpClient())
        {
            client.Connect(ntpServer, 123);
            // await client.SendAsync(data);
            // var result = await client.ReceiveAsync(this.BigMachine.Core.CancellationToken);

            var packet = NtpPacket.CreateSendPacket();
            await client.SendAsync(packet.PacketData);
            var result = await client.ReceiveAsync(this.BigMachine.Core.CancellationToken);
            var packet2 = new NtpPacket(result.Buffer);

            return packet2.ReferenceTimestamp;
        }
    }

    private ILogger<NtpMachine> logger;
    private int count = 1;
}
