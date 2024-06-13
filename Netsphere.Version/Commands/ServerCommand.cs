// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using System.Runtime.CompilerServices;
using Arc.Collections;
using Arc.Crypto;
using Arc.Threading;
using Arc.Unit;
using Netsphere.Crypto;
using Netsphere.Misc;
using Netsphere.Packet;
using Netsphere.Relay;
using Netsphere.Stats;
using SimpleCommandLine;
using Tinyhand;

namespace Netsphere.Version;

[SimpleCommand("server", Default = true)]
internal class ServerCommand : ISimpleCommandAsync<ServerOptions>
{
    private const int DelayMilliseconds = 1_000; // 1 second
    private const int NtpCorrectionCount = 3600; // 3600 x 1000ms = 1 hour

    public ServerCommand(ILogger<ServerCommand> logger, NetControl netControl, IRelayControl relayControl, NtpCorrection ntpCorrection)
    {
        this.logger = logger;
        this.netControl = netControl;
        this.relayControl = relayControl;
        this.ntpCorrection = ntpCorrection;

        this.versionData = VersionData.Load();
    }

    public async Task RunAsync(ServerOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"{options.ToString()}");

        if (!options.Check(this.logger))
        {
            return;
        }

        await this.ntpCorrection.CorrectMicsAndUnitLogger(this.logger);
        // Console.WriteLine($"{Mics.ToDateTime(Mics.GetCorrected())}");

        this.netControl.NetBase.SetRespondPacketFunc(RespondPacketFunc);
        var address = await NetStatsHelper.GetOwnAddress((ushort)options.Port);
        var token = await this.LoadToken();

        this.logger.TryGet()?.Log($"{address.ToString()}");
        this.versionData.Log(this.logger);
        this.logger.TryGet()?.Log("Press Ctrl+C to exit");

        var ntpCorrectionCount = 0;
        while (await ThreadCore.Root.Delay(1_000))
        {
            /*var keyInfo = Console.ReadKey(true);
            if (keyInfo.Key == ConsoleKey.R && keyInfo.Modifiers == ConsoleModifiers.Control)
            {// Restart
                await runner.Command.Restart();
            }
            else if (keyInfo.Key == ConsoleKey.Q && keyInfo.Modifiers == ConsoleModifiers.Control)
            {// Stop and quit
                await runner.Command.StopAll();
                runner.TerminateMachine();
            }*/

            if (ntpCorrectionCount++ >= NtpCorrectionCount)
            {
                ntpCorrectionCount = 0;
                await this.ntpCorrection.CorrectMicsAndUnitLogger(this.logger);
            }
        }
    }

    private static BytePool.RentMemory? RespondPacketFunc(ulong packetId, PacketType packetType, ReadOnlyMemory<byte> packet)
    {
        if (packetType == PacketType.GetVersion)
        {
            var versionKind = VersionInfo.Kind.Development;
            if (packet.Length >= 2)
            {
                versionKind = (VersionInfo.Kind)packet.Span[1];
            }

            if (instance?.versionData.GetVersionResponse(versionKind) is { } response)
            {
                PacketTerminal.CreatePacket(packetId, response, out var rentMemory);
                return rentMemory;
            }
        }
        else if (packetType == PacketType.UpdateVersion)
        {
            if (TinyhandSerializer.TryDeserialize<UpdateVersionPacket>(packet.Span, out var updateVersionPacket) &&
                updateVersionPacket.Token is { } token &&
                token.Target.VersionIdentifier == 0)
            {

            }
        }

        return default;
    }

    private async Task<CertificateToken<VersionInfo>?> LoadToken()
    {
        return default;
    }

    private async Task SaveToken(CertificateToken<VersionInfo> token)
    {
        var st = CryptoHelper.ConvertToUtf8(token);
    }

    private static ServerCommand? instance;

    private readonly ILogger logger;
    private readonly NetControl netControl;
    private readonly IRelayControl relayControl;
    private readonly VersionData versionData;
    private readonly NtpCorrection ntpCorrection;
}
