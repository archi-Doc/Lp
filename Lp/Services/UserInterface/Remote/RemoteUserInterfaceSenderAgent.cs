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
    private readonly LpUnit lpUnit;
    private readonly LpBase lpBase;
    private readonly ILogger logger;
    private SimpleParser? simpleParser;

    public bool IsAuthenticated { get; private set; }

    public RemoteUserInterfaceSenderAgent(LpUnit lpUnit, IServiceProvider serviceProvider, LpBase lpBase, ILogger<RemoteUserInterfaceSenderAgent> logger)
    {
        this.serviceScope = serviceProvider.CreateScope();
        this.serviceProvider = this.serviceScope.ServiceProvider;
        this.lpUnit = lpUnit;
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

        this.logger.GetWriter(LogLevel.Warning)?.Write($"Remote>> {message}");

        var receiver = clientConnection.GetService<IRemoteUserInterfaceReceiver>();
        this.Prepare(receiver);
        _ = Task.Run(async () =>
        {
            try
            {
                await this.simpleParser.ParseAndExecute(message).ConfigureAwait(false);
            }
            catch
            {
            }
            finally
            {// Return control of console input.
                await receiver.ReturnInputControl(default).ConfigureAwait(false);
            }
        });
        // _ = this.simpleParser.ParseAndRunAsync(message).ConfigureAwait(false);

        return NetResult.Success;
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
