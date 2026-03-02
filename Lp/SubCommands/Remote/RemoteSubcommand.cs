// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.NetServices;
using Netsphere.Crypto;
using SimpleCommandLine;
using SimplePrompt;

namespace Lp.Subcommands;

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

    private readonly UnitContext unitContext;
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly LpService lpService;
    private readonly NetTerminal netTerminal;
    private readonly RobustConnection.Factory robustConnectionFactory;
    private readonly SimpleConsole simpleConsole;

    public RemoteSubcommand(UnitContext unitContext, ILogger<RemoteSubcommand> logger, IUserInterfaceService userInterfaceService, LpService lpService, NetTerminal netTerminal, RobustConnection.Factory robustConnectionFactory, SimpleConsole simpleConsole)
    {
        this.unitContext = unitContext;
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.lpService = lpService;
        this.netTerminal = netTerminal;
        this.robustConnectionFactory = robustConnectionFactory;
        this.simpleConsole = simpleConsole;
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

        /*var robustConnection = this.robustConnectionFactory.Create(
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
        }*/

        var readineOptions = new ReadLineOptions()
        {
            Prompt = "Remote >> ",
            MultilineDelimiter = LpConstants.MultilineIndeitifierString,
            MultilinePrompt = LpConstants.MultilinePromptString,
        };

        using (var connection = await this.netTerminal.Connect(node, Connection.ConnectMode.NoReuse).ConfigureAwait(false))
        {
            if (connection is null)
            {// Failed to connect
                this.logger.TryGet()?.Log(Hashed.Error.Connect, node.ToString());
                return;
            }

            var clientService = connection.GetService<IRemoteUserInterfaceClient>();
            var agreement = new ConnectionAgreement();
            agreement.MinimumConnectionRetentionMics = Mics.FromMinutes(1);
            var token = CertificateToken<ConnectionAgreement>.CreateAndSign(agreement, seedKey, connection);
            var netResult = await clientService.ConnectBidirectionally(token);
            if (netResult != NetResult.Success)
            {
                this.logger.TryGet()?.Log(Hashed.Error.Connect, node.ToString());
                return;
            }

            while (!this.unitContext.Core.IsTerminated)
            {
                var result = await this.simpleConsole.ReadLine(readineOptions, this.unitContext.Core.CancellationToken).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    break;
                }

                if (string.Compare(result.Text, "exit", true) == 0)
                {// Exit
                    return;
                }
                else
                {
                    netResult = await clientService.Send(result.Text).ConfigureAwait(false);
                    if (netResult != NetResult.Success)
                    {
                        break;
                    }
                }

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
    }
}
