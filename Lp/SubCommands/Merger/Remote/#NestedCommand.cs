// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
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

    public SignaturePrivateKey RemoteKey { get; set; } = SignaturePrivateKey.Empty;
}

[SimpleCommand("merger-remote")]
public class Command : ISimpleCommandAsync<CommandOptions>
{
    public Command(ILogger<Command> logger, IUserInterfaceService userInterfaceService, NestedCommand nestedcommand, LpService lpService, RobustConnection.Terminal robustConnectionTerminal)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.nestedcommand = nestedcommand;
        this.lpService = lpService;
        this.robustConnectionTerminal = robustConnectionTerminal;
    }

    public async Task RunAsync(CommandOptions options, string[] args)
    {
        if (!string.IsNullOrEmpty(options.Node))
        {
            if (!NetNode.TryParseNetNode(this.logger, options.Node, out var node))
            {
                return;
            }

            this.nestedcommand.Node = node;
        }

        var authority = options.Authority;
        if (string.IsNullOrEmpty(authority) && args.Length > 0)
        {
            authority = args[0];
        }

        var privateKey = await this.lpService.GetSignaturePrivateKey(this.logger, authority, options.Vault, options.PrivateKey);
        if (privateKey is null)
        {
            return;
        }

        this.userInterfaceService.WriteLine(this.nestedcommand.Node.ToString());
        this.userInterfaceService.WriteLine($"Remote key: {privateKey.ToPublicKey()}");

        var robustConnection = this.robustConnectionTerminal.Open(this.nestedcommand.Node);
        var r = await robustConnection.Get();

        await this.nestedcommand.MainAsync();
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NestedCommand nestedcommand;
    private readonly LpService lpService;
    private readonly RobustConnection.Terminal robustConnectionTerminal;
}

public record CommandOptions
{
    [SimpleOption("Authority", Description = "Authority name")]
    public string Authority { get; init; } = string.Empty;

    [SimpleOption("Vault", Description = "Vault name")]
    public string Vault { get; init; } = string.Empty;

    [SimpleOption("PrivateKey", Description = "Signature private key string")]
    public string PrivateKey { get; init; } = string.Empty;

    [SimpleOption("Node", Description = "Node information")]
    public string Node { get; init; } = string.Empty;
}
