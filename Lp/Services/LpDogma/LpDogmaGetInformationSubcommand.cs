// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands;

internal static class LpDogmaHelper
{
    public static async Task<LpDogmaNetService?> TryConnect(ILogger logger, AuthorityControl authorityControl, NetTerminal netTerminal, string netNode)
    {
        if (await authorityControl.GetLpSeedKey(logger) is not { } lpSeedKey)
        {
            return default;
        }

        if (!NetNode.TryParseNetNode(logger, netNode, out var node))
        {
            return default;
        }

        using (var connection = await netTerminal.Connect(node))
        {
            if (connection is null)
            {
                logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.Connect, netNode.ToString());
                return default;
            }

            var service = connection.GetService<LpDogmaNetService>();
            var auth = AuthenticationToken.CreateAndSign(lpSeedKey, connection);
            var r = await service.Authenticate(auth);
            return service;
        }
    }
}

[SimpleCommand("lp-dogma-get-information")]
public class LpDogmaGetInformationSubcommand : ISimpleCommandAsync<ConnectNetNodeOptions>
{
    public LpDogmaGetInformationSubcommand(ILogger<LpDogmaGetInformationSubcommand> logger, AuthorityControl authorityControl, NetTerminal netTerminal)
    {
        this.logger = logger;
        this.authorityControl = authorityControl;
        this.netTerminal = netTerminal;
    }

    public async Task RunAsync(ConnectNetNodeOptions options, string[] args)
    {
        var service = await LpDogmaHelper.TryConnect(this.logger, this.authorityControl, this.netTerminal, options.NetNode);
        if (service is null)
        {
            return;
        }

        var info = await service.GetInformation();
        if (info is null)
        {
            return;
        }

        this.logger.TryGet()?.Log($"Success");

        if (info.NodeKey.IsValid)
        {
            this.logger.TryGet()?.Log($"NodeKey: {info.NodeKey}");
        }

        if (info.MergerKey.IsValid)
        {
            this.logger.TryGet()?.Log($"MergerKey: {info.MergerKey}");
        }

        if (info.RelayMergerKey.IsValid)
        {
            this.logger.TryGet()?.Log($"RelayMergerKey: {info.RelayMergerKey}");
        }

        if (info.LinkerKey.IsValid)
        {
            this.logger.TryGet()?.Log($"LinkerKey: {info.LinkerKey}");
        }
    }

    private readonly ILogger logger;
    private readonly AuthorityControl authorityControl;
    private readonly NetTerminal netTerminal;
}

public record ConnectNetNodeOptions
{
    [SimpleOption("NetNode", Description = "Net node", Required = true)]
    public string NetNode { get; init; } = string.Empty;
}
