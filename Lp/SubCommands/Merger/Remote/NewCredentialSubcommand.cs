﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerRemote;

[SimpleCommand("new-credential")]
public class NewCredentialSubcommand : ISimpleCommandAsync<NewCredentialOptions>
{
    public NewCredentialSubcommand(ILogger<NewCredentialSubcommand> logger, IUserInterfaceService userInterfaceService, NestedCommand nestedcommand)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.nestedcommand = nestedcommand;
    }

    public async Task RunAsync(NewCredentialOptions options, string[] args)
    {
        if (await this.nestedcommand.RobustConnection.GetConnection(this.logger) is not { } connection)
        {
            return;
        }

        var service = connection.GetService<IMergerRemote>();
        var r = await service.SendValueProofEvidence(default);

        this.logger.TryGet()?.Log($"{r.ToString()}");
        this.logger.TryGet()?.Log("New credential");
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NestedCommand nestedcommand;
}

public record NewCredentialOptions
{
    [SimpleOption("Authority", Description = "Authority name")]
    public string Authority { get; init; } = string.Empty;
}
