// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand(Name)]
public class FreezeSubcommand : ISimpleCommand<FreezeSubcommand.Options>
{
    public const string Name = "freeze";

    public record Options
    {
        [SimpleOption("Duration", Description = "Duration")]
        public int Duration { get; init; } = 3;
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;

    public FreezeSubcommand(ILogger<TemplateSubcommand> logger, IUserInterfaceService userInterfaceService)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
    }

    public async Task Execute(Options options, string[] args, CancellationToken cancellationToken)
    {
        var duration = options.Duration;
        if (args.Length > 0 &&
            int.TryParse(args[0], out var x))
        {
            duration = x;
        }

        this.userInterfaceService.WriteLine($"Freeze for {duration} seconds");
        await Task.Delay(duration * 1000, cancellationToken);
        this.userInterfaceService.WriteLine($"Freeze completed");
    }
}
