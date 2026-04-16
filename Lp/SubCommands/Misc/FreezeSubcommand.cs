// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand(Name)]
public class FreezeSubcommand : ISimpleCommandAsync<FreezeSubcommand.Options>
{
    public const string Name = "freeze";

    public record Options
    {
        [SimpleOption("Duration", Description = "Duration", Required = true)]
        public int Duration { get; init; } = 3;
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;

    public FreezeSubcommand(ILogger<TemplateSubcommand> logger, IUserInterfaceService userInterfaceService)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
    }

    public async Task RunAsync(Options options, string[] args)
    {
        this.userInterfaceService.WriteLine($"Freeze for {options.Duration} seconds");
        await Task.Delay(options.Duration * 1000);
        this.userInterfaceService.WriteLine($"Freeze completed");
    }
}
