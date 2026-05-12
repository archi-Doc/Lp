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
    private readonly IServiceScope serviceScope;
    private readonly IServiceProvider serviceProvider;
    private readonly LpBase lpBase;
    private readonly ILogger logger;
    private SimpleParser? simpleParser;

    private ExecutionRoot root;
    private ExecutionGroup? remoteGroup;

    public bool IsAuthenticated { get; private set; }

    public RemoteUserInterfaceSenderAgent(ExecutionRoot root, /*ExecutionStack executionStack, */IServiceProvider serviceProvider, LpBase lpBase, ILogger<RemoteUserInterfaceSenderAgent> logger)
    {
        this.root = root;
        this.serviceScope = serviceProvider.CreateScope();
        this.serviceProvider = this.serviceScope.ServiceProvider;
        this.lpBase = lpBase;
        this.logger = logger;
    }

    void INetObject.OnConnectionClosed()
    {
        this.serviceScope.Dispose();
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

    async Task<NetResult> IRemoteUserInterfaceSender.Send(int id, string message)
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

        /*var context = this.remoteStack.TryPush(id, this.remoteStack.Root, default);//
        if (context is null)
        {
            return NetResult.Refused;
        }*/

        // Not thread-safe
        var group = this.remoteGroup;
        if (group is null)
        {
            group = new(this.root);
            group.Id = id;
            this.remoteGroup = group;
        }

        this.logger.GetWriter(LogLevel.Warning)?.Write($"Remote >> {message}");

        var receiver = clientConnection.GetService<IRemoteUserInterfaceReceiver>();
        this.Prepare(receiver);
        _ = Task.Run(async () =>
        {
            try
            {
                await this.simpleParser.ParseAndExecute(message, group.CancellationToken).WaitAsync(clientConnection.Agreement.TransmissionTimeout).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                this.logger.GetWriter(LogLevel.Warning)?.Write("Timeout");
            }
            finally
            {
                group.Dispose();
                this.remoteGroup = default;

                // Return control of console input.
                await receiver.ReturnInputControl(id).ConfigureAwait(false);
            }
        });
        // _ = this.simpleParser.ParseAndRunAsync(message).ConfigureAwait(false);

        return NetResult.Success;
    }

    Task<NetResult> IRemoteUserInterfaceSender.Cancel(int id)
    {
        if (!this.IsAuthenticated ||
            TransmissionContext.Current.ServerConnection.BidirectionalConnection is not { } clientConnection)
        {
            return Task.FromResult(NetResult.NotAuthenticated);
        }

        if (this.remoteGroup is { } group)
        {
            if (group.IsTerminated)
            {
                this.remoteGroup = default;
                return Task.FromResult(NetResult.Refused);
            }

            if (id != group.Id)
            {
                return Task.FromResult(NetResult.Refused);
            }

            try
            {
                group.Dispose();
            }
            finally
            {
                this.remoteGroup = default;
            }
        }

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
