// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.T3cs;

[SimpleCommand("inspect-owner")]
public class InspectOwnerSubcommand : ISimpleCommandAsync<InspectOwnerOptions>
{
    public InspectOwnerSubcommand(IUserInterfaceService userInterfaceService, ILogger<InspectOwnerOptions> logger, LpService lpService)
    {
        this.userInterfaceService = userInterfaceService;
        this.logger = logger;
        this.lpService = lpService;
    }

    public async Task RunAsync(InspectOwnerOptions option, string[] args)
    {
        var r = await this.lpService.ParseSeedKeyAndCredit(option.Source);
        if (!r.IsSuccess)
        {
            this.userInterfaceService.WriteLine(HashedString.FromEnum(r.Code));
            return;
        }

        var credential = this.lpService.ResolveMerger(r.Credit);
        if (credential is null)
        {
            this.userInterfaceService.WriteLine("Credential not found.");
            return;
        }

        //credential.Proof.State.NetNode
    }

    private readonly IUserInterfaceService userInterfaceService;
    private readonly ILogger logger;
    private readonly LpService lpService;
}

public record InspectOwnerOptions
{
    [SimpleOption("Source", Description = "Authority@Identifier/Mergers", Required = true)]
    public string Source { get; init; } = string.Empty;
}
