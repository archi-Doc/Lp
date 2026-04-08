// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.OperateCredit;

[SimpleCommand("test")]
public class TestSubcommand : ISimpleCommandAsync
{
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NestedCommand nestedcommand;

    public TestSubcommand(ILogger<TestSubcommand> logger, IUserInterfaceService userInterfaceService, NestedCommand nestedcommand)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.nestedcommand = nestedcommand;
    }

    public async Task RunAsync(string[] args)
    {
        this.logger.GetWriter()?.Write($"Log");
        this.userInterfaceService.WriteLine("UI");
    }
}
