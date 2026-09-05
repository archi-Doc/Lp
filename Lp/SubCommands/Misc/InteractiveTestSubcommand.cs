// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Stats;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand(Name)]
public class InteractiveTestSubcommand : ISimpleCommand
{
    public const string Name = "interactive-test";

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NetStats netStats;

    public InteractiveTestSubcommand(ILogger<InteractiveTestSubcommand> logger, IUserInterfaceService userInterfaceService, NetStats netStats)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.netStats = netStats;
    }

    public async Task Execute(string[] args, CancellationToken cancellationToken)
    {
        this.userInterfaceService.WriteLine(LogLevel.Information, "Interactive test");
        this.userInterfaceService.WriteLineWarning("Warning text");

        var result = await this.userInterfaceService.ReadLine(false, "Enter > ", cancellationToken);
        this.userInterfaceService.WriteLine($"ReadLine: {result.ToString()}");
        this.userInterfaceService.WriteLine($"Address: {this.netStats.OwnNetNode?.ToString()}");
        this.userInterfaceService.WriteLine();

        var result2 = await this.userInterfaceService.ReadYesNo(true, "Yes or No?");
        if (result2 == InputResultKind.Success)
        {
            this.userInterfaceService.WriteLine($"Yes!");
        }
        else
        {
            this.userInterfaceService.WriteLine($"No...");
        }
    }
}
