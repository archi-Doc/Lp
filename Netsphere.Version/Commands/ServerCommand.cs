// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using Arc.Crypto;
using Arc.Threading;
using Arc.Unit;
using Netsphere.Crypto;
using Netsphere.Relay;
using Netsphere.Stats;
using SimpleCommandLine;
using Tinyhand;

namespace Netsphere.Version;

[SimpleCommand("server", Default = true)]
internal class ServerCommand : ISimpleCommandAsync<ServerOptions>
{
    public ServerCommand(ILogger<ServerCommand> logger, NetControl netControl, IRelayControl relayControl)
    {
        this.logger = logger;
        this.netControl = netControl;
        this.relayControl = relayControl;
    }

    public async Task RunAsync(ServerOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"{options.ToString()}");

        if (!options.Check(this.logger))
        {
            return;
        }

        var address = await NetStatsHelper.GetOwnAddress((ushort)options.Port);
        var token = await this.LoadToken();

        this.logger.TryGet()?.Log($"{address.ToString()}");
        this.logger.TryGet()?.Log("Press Ctrl+C to exit");

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
        }
    }

    private async Task<CertificateToken<VersionInfo>?> LoadToken()
    {
        return default;
    }

    private async Task SaveToken(CertificateToken<VersionInfo> token)
    {
        var st = CryptoHelper.ConvertToUtf8(token);
    }

    private readonly ILogger logger;
    private readonly NetControl netControl;
    private readonly IRelayControl relayControl;
}
