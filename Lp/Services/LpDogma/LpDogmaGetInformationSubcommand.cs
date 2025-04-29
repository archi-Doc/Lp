// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands;

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
        using (var connectionAndService = await LpDogmaHelper.TryConnect(this.logger, this.authorityControl, this.netTerminal, options.NetNode))
        {
            if (connectionAndService.IsInvalid)
            {
                return;
            }

            var info = await connectionAndService.Service.GetInformation();
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
