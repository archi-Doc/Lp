// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net.Sockets;
using Netsphere;

namespace LP.Machines;

[MachineObject(0x5e1f81ca, Group = typeof(SingleGroup<>))]
public partial class NtpMachine : Machine<Identifier>
{
    private const string TimestampFormat = "MM-dd HH:mm:ss.fff K";

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
        var packet = await this.GetTime(parameter.CancellationToken);
        this.logger.TryGet(LogLevel.Information)?.Log($"RoundtripTime: {packet.RoundtripTime.ToString()}, TimeOffset: {packet.TimeOffset.ToString()}");
        this.logger.TryGet(LogLevel.Information)?.Log($"Server: {packet.TransmitTimestamp.ToString(TimestampFormat)}, Corrected: {packet.CorrectedUtcNow.ToString(TimestampFormat)}");

        return StateResult.Continue;
    }

    private async Task<NtpPacket> GetTime(CancellationToken cancellationToken)
    {
        const string ntpServer = "time.google.com";

        using (var client = new UdpClient())
        {
            client.Connect(ntpServer, 123);

            var packet = NtpPacket.CreateSendPacket();
            await client.SendAsync(packet.PacketData, cancellationToken);
            var result = await client.ReceiveAsync(cancellationToken);
            var packet2 = new NtpPacket(result.Buffer);

            return packet2;
        }
    }

    private ILogger<NtpMachine> logger;
}
