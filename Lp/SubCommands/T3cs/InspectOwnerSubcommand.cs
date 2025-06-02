// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.T3cs;

[SimpleCommand("inspect-owner")]
public class InspectOwnerSubcommand : ISimpleCommandAsync<InspectOwnerOptions>
{
    public InspectOwnerSubcommand(ILogger<InspectOwnerOptions> logger, LpService lpService)
    {
        this.logger = logger;
        this.lpService = lpService;
    }

    public async Task RunAsync(InspectOwnerOptions option, string[] args)
    {
        var r = await this.lpService.ParseSeedKeyAndCredit(this.logger, option.Source);
    }

    private readonly ILogger logger;
    private readonly LpService lpService;
}

public record InspectOwnerOptions
{
    [SimpleOption("Source", Description = "Authority@Identifier/Mergers", Required = true)]
    public string Source { get; init; } = string.Empty;
}
