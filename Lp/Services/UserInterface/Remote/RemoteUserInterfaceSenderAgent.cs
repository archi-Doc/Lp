// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.Services;
using Lp.Subcommands;
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

    public bool IsAuthenticated { get; private set; }

    public RemoteUserInterfaceSenderAgent(IServiceProvider serviceProvider, LpBase lpBase, ILogger<RemoteUserInterfaceSenderAgent> logger)
    {
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

    async Task<NetResult> IRemoteUserInterfaceSender.Send(string message)
    {
        if (!this.IsAuthenticated)
        {
            return NetResult.NotAuthenticated;
        }

        if (TransmissionContext.Current.ServerConnection.BidirectionalConnection is not { } clientConnection)
        {
            return NetResult.NotAuthenticated;
        }

        this.Prepare(clientConnection);
        _ = this.simpleParser.ParseAndRunAsync(message).ConfigureAwait(false);

        return NetResult.Success;
    }

    [MemberNotNull(nameof(simpleParser))]
    private void Prepare(ClientConnection clientConnection)
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

        Type[] subcommands = [typeof(InspectSubcommand),];

        this.serviceProvider.GetRequiredService<UserInterfaceServiceContext>().InitializeRemote(clientConnection.GetService<IRemoteUserInterfaceReceiver>());
        this.simpleParser = new SimpleParser(subcommands, subcommandOptions);
    }
}
