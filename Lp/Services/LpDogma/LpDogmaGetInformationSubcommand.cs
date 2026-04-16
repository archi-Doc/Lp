// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("lp-dogma-get-information")]
public class LpDogmaGetInformationSubcommand : ISimpleCommand<ConnectNetNodeOptions>
{
    public LpDogmaGetInformationSubcommand(ILogger<LpDogmaGetInformationSubcommand> logger, AuthorityControl authorityControl, NetTerminal netTerminal)
    {
        this.logger = logger;
        this.authorityControl = authorityControl;
        this.netTerminal = netTerminal;
    }

    public async Task Execute(ConnectNetNodeOptions options, string[] args, CancellationToken cancellationToken)
    {
        using (var connectionAndService = await LpDogmaHelper.TryConnect(this.logger, this.authorityControl, this.netTerminal, options.NetNode))
        {
            if (connectionAndService.IsFailure)
            {
                return;
            }

            var info = await connectionAndService.Service.GetInformation();
            if (info is null)
            {
                return;
            }

            this.logger.GetWriter()?.Write($"Success");

            if (info.NodeKey.IsValid)
            {
                this.logger.GetWriter()?.Write($"NodeKey: {info.NodeKey}");
            }

            if (info.MergerKey.IsValid)
            {
                this.logger.GetWriter()?.Write($"MergerKey: {info.MergerKey}");
            }

            if (info.RelayMergerKey.IsValid)
            {
                this.logger.GetWriter()?.Write($"RelayMergerKey: {info.RelayMergerKey}");
            }

            if (info.LinkerKey.IsValid)
            {
                this.logger.GetWriter()?.Write($"LinkerKey: {info.LinkerKey}");
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
