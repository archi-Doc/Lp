// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.NetServices;
using Lp.Services;
using Microsoft.Extensions.DependencyInjection;
using Netsphere.Crypto;
using SimpleCommandLine;
using SimplePrompt;

namespace Lp.Subcommands;

[SimpleCommand("remote")]
public class RemoteSubcommand : ISimpleCommand<RemoteSubcommand.Options>
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
    private readonly LpUnit lpUnit;
    private readonly LpService lpService;
    private readonly NetTerminal netTerminal;
    // private readonly RobustConnection.Factory robustConnectionFactory;
    // private readonly SimpleConsole simpleConsole;
    private readonly ExecutionStack executionStack;

    public RemoteSubcommand(UnitContext unitContext, LpUnit lpUnit, ILogger<RemoteSubcommand> logger, IUserInterfaceService userInterfaceService, LpService lpService, NetTerminal netTerminal, ExecutionStack executionStack)
    {
        this.unitContext = unitContext;
        this.lpUnit = lpUnit;
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.lpService = lpService;
        this.netTerminal = netTerminal;
        // this.robustConnectionFactory = robustConnectionFactory;
        this.executionStack = executionStack;
    }

    public async Task Execute(Options options, string[] args, CancellationToken cancellationToken)
    {
        var parent = cancellationToken.ExtractCore();
        if (parent is null)
        {
            return;
        }

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
            this.logger.GetWriter()?.Write(Hashed.Error.Connect, node.ToString());
            return;
        }*/

        using (var connection = await this.netTerminal.Connect(node, Connection.ConnectMode.NoReuse).ConfigureAwait(false))
        {
            if (connection is null)
            {// Failed to connect
                this.logger.GetWriter()?.Write(Hashed.Error.Connect, node.ToString());
                return;
            }

            var senderService = connection.GetService<IRemoteUserInterfaceSender>();
            var agreement = new ConnectionAgreement();
            agreement.MinimumConnectionRetentionMics = Mics.FromMinutes(1);
            agreement.TransmissionTimeout = TimeSpan.FromMinutes(1);
            var token = CertificateToken<ConnectionAgreement>.CreateAndSign(agreement, seedKey, connection);

            // // Customized ConnectBidirectionally()
            var serverConnection = connection.PrepareBidirectionalConnection();
            var resultAndValue = await senderService.ConnectBidirectionally(token);
            if (resultAndValue.IsSuccessAndValid)
            {
                connection.Agreement.EnableBidirectionalConnection = true;
                connection.Agreement.AcceptAll(token.Target);
            }
            else
            {
                this.logger.GetWriter()?.Write(Hashed.Error.Connect, node.ToString());
                return;
            }

            this.logger.GetWriter()?.Write(Hashed.Success.Connect, node.ToString());
            var nodeName = resultAndValue.Value;
            if (nodeName.Length > Alias.MaxAliasLength)
            {
                nodeName = nodeName.Substring(0, Alias.MaxAliasLength);
            }

            var context = serverConnection.GetContext();
            context.EnableNetService<IRemoteUserInterfaceReceiver>();
            if (context.GetOrCreateNetService<IRemoteUserInterfaceReceiver>() is not { } receiver)
            {//
                return;
            }

            receiver.OutputPrefix = $"[{nodeName}] ";
            receiver.InputPrefix = $"{nodeName} >> ";

            using (var executionContext = this.executionStack.Push(parent, (x, signal) =>
            {
                if (signal == ExecutionSignal.Exit)
                {
                    x.RequestTermination();
                }
            }))
            {
                while (executionContext.CanContinue)
                {
                    var result = await this.userInterfaceService.ReadLine(false, receiver.InputPrefix, executionContext.CancellationToken).ConfigureAwait(false);
                    // var result = await this.simpleConsole.ReadLine(readineOptions, scope.CancellationToken).ConfigureAwait(false);
                    if (!result.IsSuccess)
                    {
                        break;
                    }

                    if (string.Compare(result.Text, "exit", true) == 0)
                    {// Exit
                        return;
                    }

                    using (var executionContext2 = this.executionStack.Push(executionContext, (x, signal) =>
                    {
                        if (signal == ExecutionSignal.Cancel)
                        {
                            senderService.Cancel(x.Id);
                            x.RequestTermination(); // Perform cancellation in advance in case the network is disconnected.
                            this.userInterfaceService.WriteLineError(Hashed.Dialog.Canceled);
                        }
                    }))
                    {
                        receiver.Id = executionContext2.Id;

                        var netResult = await senderService.Send(executionContext2.Id, result.Text).ConfigureAwait(false);
                        if (netResult != NetResult.Success)
                        {
                            this.userInterfaceService.WriteLineError(HashedString.FromEnum(netResult));
                            break;
                        }

                        try
                        {
                            await executionContext2.Completion.WaitAsync(executionContext2.CancellationToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        finally
                        {
                            executionContext2.RequestTermination();
                        }
                    }
                }
            }

            this.userInterfaceService.WriteLineError(Hashed.Dialog.Exit);
            await Task.Delay(LpParameters.ExitDelayMilliseconds);
        }
    }
}
