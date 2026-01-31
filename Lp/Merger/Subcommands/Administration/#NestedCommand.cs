// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using Netsphere;
using Netsphere.Crypto;
using SimpleCommandLine;
using SimplePrompt;

namespace Lp.Subcommands.MergerRemote;

public class NestedCommand : NestedCommand<NestedCommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var t = typeof(NestedCommand);
        context.TryAddSingleton(t);

        var group = context.GetCommandGroup(t);
        // group.AddCommand(typeof(LpNewCredentialSubcommand));
        group.AddCommand(typeof(ShowMergerKeySubcommand));
        group.AddCommand(typeof(CreateCreditSubcommand));
    }

    public NestedCommand(UnitContext context, UnitCore core, SimpleConsole simpleConsole)
        : base(context, core, simpleConsole)
    {
        this.ReadLineOptions = new ReadLineOptions
        {
            Prompt = "merger-admin>> ",
            MultilinePrompt = LpConstants.MultilinePromptString,
        };
    }

    public RobustConnection? RobustConnection { get; set; }

    public SeedKey? RemoteKey { get; set; }
}

[SimpleCommand("merger-admin")]
public class Command : ISimpleCommandAsync<CommandOptions>
{
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NestedCommand nestedcommand;
    private readonly LpService lpService;
    private readonly RobustConnection.Factory robustConnectionFactory;

    public Command(ILogger<Command> logger, IUserInterfaceService userInterfaceService, NestedCommand nestedcommand, LpService lpService, RobustConnection.Factory robustConnectionFactory)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.nestedcommand = nestedcommand;
        this.lpService = lpService;
        this.robustConnectionFactory = robustConnectionFactory;
    }

    public async Task RunAsync(CommandOptions options, string[] args)
    {
        if (!NetNode.TryParseNetNode(this.logger, options.Node, out var node))
        {
            return;
        }

        /*var code = options.Code;
        if (string.IsNullOrEmpty(code) && args.Length > 0)
        {
            code = args[0];
        }*/

        // Code
        var seedKey = await this.lpService.GetSeedKeyFromCode(options.Code);
        if (seedKey is null)
        {
            return;
        }

        this.userInterfaceService.WriteLine($"Node: {node.ToString()}");
        this.userInterfaceService.WriteLine($"Remote key: {seedKey.GetSignaturePublicKey()}");

        this.nestedcommand.RobustConnection = this.robustConnectionFactory.Create(
            node,
            new(
                async connection =>
                {
                    var token = AuthenticationToken.CreateAndSign(seedKey, connection);
                    var r = await connection.GetService<IMergerAdministration>().Authenticate(token);
                    if (r.IsSuccess)
                    {
                        connection.Agreement.AcceptAll(r.Value);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }));

        if (await this.nestedcommand.RobustConnection.Get() is not { } connection)
        {
            this.logger.TryGet()?.Log(Hashed.Error.Connect, node.ToString());
            return;
        }

        this.userInterfaceService.WriteLine($"Retention: {connection.Agreement.MinimumConnectionRetentionMics.MicsToTimeSpanString()}");
        this.userInterfaceService.WriteLine($"Connection successful (merger-admin)");

        await this.nestedcommand.MainAsync();
    }
}

public record CommandOptions
{
    [SimpleOption("Node", Description = "Node information", Required = true)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("Code", Description = "Remote code (secret key, vault, authority)", Required = true)]
    public string Code { get; init; } = string.Empty;

    // [SimpleOption("PrivateKey", Description = "Signature private key string")]
    // public string PrivateKey { get; init; } = string.Empty;
}
