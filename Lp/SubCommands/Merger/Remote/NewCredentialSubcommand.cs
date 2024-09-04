// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerRemote;

[SimpleCommand("new-credential")]
public class NewCredentialSubcommand : ISimpleCommandAsync<NewCredentialOptions>
{
    public NewCredentialSubcommand(ILogger<MergerClient.Command> logger, IUserInterfaceService userInterfaceService, NestedCommand nestedcommand)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.nestedcommand = nestedcommand;
    }

    public async Task RunAsync(NewCredentialOptions options, string[] args)
    {
        if (this.nestedcommand.RobustConnection is null ||
            await this.nestedcommand.RobustConnection.Get() is not { } connection)
        {
            return;
        }

        var service = connection.GetService<IMergerRemote>();
        var r = await service.SendValueProofEvidence(1);

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
