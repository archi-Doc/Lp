// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.Services;
using Microsoft.Extensions.DependencyInjection;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.NetServices;

[NetObject]
public partial class RemoteUserInterfaceSenderAgent : IRemoteUserInterfaceSender, INetObject
{
    private static readonly ExecutionStack RemoteStack = new(3);
    // private readonly ExecutionStack executionStack;
    private readonly IServiceScope serviceScope;
    private readonly IServiceProvider serviceProvider;
    private readonly LpBase lpBase;
    private readonly ILogger logger;
    private SimpleParser? simpleParser;

    public bool IsAuthenticated { get; private set; }

    public RemoteUserInterfaceSenderAgent(/*ExecutionStack executionStack, */IServiceProvider serviceProvider, LpBase lpBase, ILogger<RemoteUserInterfaceSenderAgent> logger)
    {
        // this.executionStack = executionStack;
        this.serviceScope = serviceProvider.CreateScope();
        this.serviceProvider = this.serviceScope.ServiceProvider;
        this.lpBase = lpBase;
        this.logger = logger;
    }

    void INetObject.OnConnectionClosed()
    {
        this.serviceScope.Dispose();
        Console.WriteLine("Server IServiceScope Disposed");
    }

    async Task<NetResultAndValue<string>> IRemoteUserInterfaceSender.ConnectBidirectionally(CertificateToken<ConnectionAgreement> token)
    {
        var serverConnection = TransmissionContext.Current.ServerConnection;
        if (token is null ||
            !token.ValidateAndVerify(serverConnection) ||
            !token.PublicKey.Equals(this.lpBase.RemotePublicKey))
        {
            return new(NetResult.NotAuthenticated, string.Empty);
        }

        serverConnection.Agreement.AcceptAll(token.Target); // Customized ConnectBidirectionally()
        TransmissionContext.Current.ServerConnection.PrepareBidirectionalConnection();

        this.IsAuthenticated = true;
        this.logger.GetWriter(LogLevel.Warning)?.Write($"Connected from {serverConnection.DestinationNode}");

        return new(NetResult.Success, this.lpBase.NodeName);
    }

    async Task<NetResult> IRemoteUserInterfaceSender.Send(long id, string message)
    {
        if (!this.IsAuthenticated ||
            TransmissionContext.Current.ServerConnection.BidirectionalConnection is not { } clientConnection)
        {
            return NetResult.NotAuthenticated;
        }

        if (id == 0)
        {
            return NetResult.InvalidData;
        }

        var scope = RemoteStack.TryPush(id, default);
        if (scope is null)
        {
            return NetResult.Refused;
        }

        this.logger.GetWriter(LogLevel.Warning)?.Write($"Remote >> {message}");

        var receiver = clientConnection.GetService<IRemoteUserInterfaceReceiver>();
        this.Prepare(receiver);
        _ = Task.Run(async () =>
        {
            try
            {//Timeout
                await this.simpleParser.ParseAndExecute(message, scope.CancellationToken).WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                this.logger.GetWriter(LogLevel.Warning)?.Write("Timeout");
            }
            finally
            {
                scope.Dispose();

                // Return control of console input.
                await receiver.ReturnInputControl(id).ConfigureAwait(false);
            }
        });
        // _ = this.simpleParser.ParseAndRunAsync(message).ConfigureAwait(false);

        return NetResult.Success;
    }

    Task<NetResult> IRemoteUserInterfaceSender.Cancel(long id)
    {
        if (!this.IsAuthenticated ||
            TransmissionContext.Current.ServerConnection.BidirectionalConnection is not { } clientConnection)
        {
            return Task.FromResult(NetResult.NotAuthenticated);
        }

        if (id == 0)
        {
            return Task.FromResult(NetResult.InvalidData);
        }

        var scope = RemoteStack.Find(id);
        if (scope is null)
        {
            return Task.FromResult(NetResult.NotFound);
        }

        scope.TryCancel();

        return Task.FromResult(NetResult.Success);
    }

    [MemberNotNull(nameof(simpleParser))]
    private void Prepare(IRemoteUserInterfaceReceiver receiver)
    {
        if (this.simpleParser is not null)
        {
            return;
        }

        var subcommandOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = this.serviceProvider,
            RequireStrictCommandName = true,
            RequireStrictOptionName = true,
            DoNotDisplayUsage = true,
            DisplayCommandListAsHelp = true,
            AutoAlias = true,
        };

        this.serviceProvider.GetRequiredService<UserInterfaceServiceContext>().InitializeRemote(receiver);
        this.simpleParser = new SimpleParser(LpUnit.RemoteSubcommands, subcommandOptions);
    }
}
