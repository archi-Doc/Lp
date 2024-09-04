// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands.MergerRemote;

public class NestedCommand : NestedCommand<NestedCommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var t = typeof(NestedCommand);
        context.TryAddSingleton(t);

        var group = context.GetCommandGroup(t);
        group.AddCommand(typeof(NewCredentialSubcommand));
    }

    public NestedCommand(UnitContext context, UnitCore core, IUserInterfaceService userInterfaceService)
        : base(context, core, userInterfaceService)
    {
    }

    public override string Prefix => "merger-remote >> ";

    public NetNode Node { get; set; } = NetNode.Alternative;
}

[SimpleCommand("merger-remote")]
public class Command : ISimpleCommandAsync<CommandOptions>
{
    public Command(ILogger<Command> logger, IUserInterfaceService userInterfaceService, NestedCommand nestedcommand)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.nestedcommand = nestedcommand;
    }

    public async Task RunAsync(CommandOptions options, string[] args)
    {
        if (!NetNode.TryParseNetNode(this.logger, options.Node, out var node))
        {
            return;
        }

        this.nestedcommand.Node = node;
        this.userInterfaceService.WriteLine(node.ToString());
        await this.nestedcommand.MainAsync();
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NestedCommand nestedcommand;
}

public record CommandOptions
{
    [SimpleOption("Node", Description = "Node information", Required = true)]
    public string Node { get; init; } = string.Empty;
}
