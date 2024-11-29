// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using SimpleCommandLine;

namespace Lp.Subcommands.MergerClient;

public class NestedCommand
    : NestedCommand<NestedCommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var t = typeof(NestedCommand);
        context.TryAddSingleton(t);

        var group = context.GetCommandGroup(t);
        group.AddCommand(typeof(InfoCommand));
        group.AddCommand(typeof(CreateCreditCommand));
    }

    public NestedCommand(UnitContext context, UnitCore core, IUserInterfaceService userInterfaceService)
        : base(context, core, userInterfaceService)
    {
    }

    public override string Prefix => "merger-client >> ";

    public RobustConnection? RobustConnection { get; set; }

    public Authority? Authority { get; set; }
}

[SimpleCommand("merger-client")]
public class Command : ISimpleCommandAsync<CommandOptions>
{
    public Command(ILogger<Command> logger, IUserInterfaceService userInterfaceService, AuthorityControl authorityControl, NestedCommand nestedcommand, RobustConnection.Factory robustConnectionFactory)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.authorityControl = authorityControl;
        this.nestedcommand = nestedcommand;
        this.robustConnectionFactory = robustConnectionFactory;
    }

    public async Task RunAsync(CommandOptions options, string[] args)
    {
        NetNode? node = Alternative.NetNode;
        if (!string.IsNullOrEmpty(options.Node))
        {
            if (!NetNode.TryParseNetNode(this.logger, options.Node, out var n))
            {
                return;
            }

            node = n;
        }

        this.nestedcommand.Authority = await this.authorityControl.GetAuthority(options.Authority);
        if (this.nestedcommand.Authority is null)
        {
            this.logger?.TryGet(LogLevel.Error)?.Log(Hashed.Authority.NotFound, options.Authority);
            return;
        }

        this.nestedcommand.RobustConnection = this.robustConnectionFactory.Create(node, x => VerificationHelper.SetAuthenticationToken(x, this.nestedcommand.Authority));
        // this.nestedcommand.RobustConnection = this.robustConnectionFactory.Create(node, x => RobustConnection.SetAuthenticationToken(x, authority.UnsafeGetPrivateKey()));
        this.userInterfaceService.WriteLine(node.ToString());
        await this.nestedcommand.MainAsync();
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly AuthorityControl authorityControl;
    private readonly NestedCommand nestedcommand;
    private readonly RobustConnection.Factory robustConnectionFactory;
}

public record CommandOptions
{
    [SimpleOption("Node", Description = "Node information", Required = true)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("Authority", Description = "Authority name", Required = true)]
    public string Authority { get; init; } = string.Empty;

    public override string ToString() => $"{this.Node}";
}
