// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;
using Lp.T3cs;
using Microsoft.Extensions.DependencyInjection;
using Netsphere.Crypto;
using SimpleCommandLine;
using SimplePrompt;

namespace Lp.Subcommands;

public class RemoteCommand : NestedCommand<RemoteCommand>
{
    public static void Configure(IUnitConfigurationContext context)
    {
        var t = typeof(RemoteCommand);
        context.TryAddSingleton(t);

        var group = context.GetCommandGroup(t);
        // group.AddCommand(typeof(LpNewCredentialSubcommand));
        group.AddCommand(typeof(ShowMergerKeySubcommand));
        group.AddCommand(typeof(CreateCreditSubcommand));
    }

    public RemoteCommand(UnitContext context, UnitCore core, SimpleConsole simpleConsole)
        : base(context, core, simpleConsole)
    {
        this.ReadLineOptions = new ReadLineOptions
        {
            Prompt = "remote>> ",
            MultilinePrompt = LpConstants.MultilinePromptString,
        };
    }

    public RobustConnection? RobustConnection { get; set; }

    // public SeedKey? RemoteKey { get; set; }
}

[SimpleCommand("remote")]
public class RemoteSubcommand : ISimpleCommandAsync<RemoteSubcommand.Options>
{
    public record Options
    {
        [SimpleOption("Node", Description = "Node information", Required = true)]
        public string Node { get; init; } = string.Empty;

        [SimpleOption("Code", Description = "Remote code (secret key, vault, authority)", Required = true)]
        public string Code { get; init; } = string.Empty;
    }

    private readonly IServiceProvider serviceProvider;
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly LpService lpService;
    private readonly RobustConnection.Factory robustConnectionFactory;
    private readonly RemoteCommand remoteCommand;

    public RemoteSubcommand(IServiceProvider serviceProvider, ILogger<RemoteSubcommand> logger, IUserInterfaceService userInterfaceService, LpService lpService, RobustConnection.Factory robustConnectionFactory, RemoteCommand remoteCommand)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.lpService = lpService;
        this.robustConnectionFactory = robustConnectionFactory;
        this.remoteCommand = remoteCommand;
    }

    public async Task RunAsync(Options options, string[] args)
    {
        if (!NetNode.TryParseNetNode(this.logger, options.Node, out var node))
        {
            return;
        }

        // Code
        var seedKey = await this.lpService.GetSeedKeyFromCode(options.Code);
        if (seedKey is null)
        {
            return;
        }

        this.userInterfaceService.WriteLine($"Node: {node.ToString()}");
        this.userInterfaceService.WriteLine($"Remote key: {seedKey.GetSignaturePublicKey()}");

        var robustConnection = this.robustConnectionFactory.Create(
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

        if (await robustConnection.Get() is not { } connection)
        {
            this.logger.TryGet()?.Log(Hashed.Error.Connect, node.ToString());
            return;
        }

        this.remoteCommand.MainAsync

        /*using (var scope = this.serviceProvider.CreateScope())
        {
            var userInterfaceContext = scope.ServiceProvider.GetRequiredService<UserInterfaceContext>();
            if (userInterfaceContext.InitializeRemote(connection))
            {

            }
        }*/

        /*this.userInterfaceService.WriteLine($"Retention: {connection.Agreement.MinimumConnectionRetentionMics.MicsToTimeSpanString()}");
        this.userInterfaceService.WriteLine($"Connection successful (merger-admin)");

        await this.nestedcommand.MainAsync();*/
    }
}
