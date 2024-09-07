// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere;
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
        group.AddCommand(typeof(ShowStateSubcommand));
    }

    public NestedCommand(UnitContext context, UnitCore core, IUserInterfaceService userInterfaceService)
        : base(context, core, userInterfaceService)
    {
    }

    public override string Prefix => "merger-remote >> ";

    public RobustConnection? RobustConnection { get; set; }

    public SignaturePrivateKey RemoteKey { get; set; } = SignaturePrivateKey.Empty;
}

[SimpleCommand("merger-remote")]
public class Command : ISimpleCommandAsync<CommandOptions>
{
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
        NetNode? node = NetNode.Alternative;
        if (!string.IsNullOrEmpty(options.Node))
        {
            if (!NetNode.TryParseNetNode(this.logger, options.Node, out var n))
            {
                return;
            }

            node = n;
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

        this.userInterfaceService.WriteLine(node.ToString());
        this.userInterfaceService.WriteLine($"Remote key: {privateKey.ToPublicKey()}");

        this.nestedcommand.RobustConnection = this.robustConnectionFactory.Create(
            node,
            new(
                async connection =>
                {
                    var token = new AuthenticationToken(connection.Salt);
                    token.Sign(privateKey);
                    return await connection.GetService<IMergerRemote>().Authenticate(token) == NetResult.Success;
                }));

        if (await this.nestedcommand.RobustConnection.Get() is null)
        {
            this.logger.TryGet()?.Log(Hashed.Error.Connect, node.ToString());
            return;
        }

        await this.nestedcommand.MainAsync();
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NestedCommand nestedcommand;
    private readonly LpService lpService;
    private readonly RobustConnection.Factory robustConnectionFactory;
}

public record CommandOptions
{
    [SimpleOption("Node", Description = "Node information", Required = true)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("Authority", Description = "Authority name")]
    public string Authority { get; init; } = string.Empty;

    [SimpleOption("Vault", Description = "Vault name")]
    public string Vault { get; init; } = string.Empty;

    [SimpleOption("PrivateKey", Description = "Signature private key string")]
    public string PrivateKey { get; init; } = string.Empty;
}
