// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.Services;
using Lp.Subcommands;
using Microsoft.Extensions.DependencyInjection;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.NetServices;

[NetObject]
public partial class RemoteUserInterfaceSender : IRemoteUserInterfaceSender, INetObject
{
    private readonly IServiceScope serviceScope;
    private readonly IServiceProvider serviceProvider;
    private readonly LpBase lpBase;
    private SimpleParser? simpleParser;

    public bool IsAuthenticated { get; private set; }

    public RemoteUserInterfaceSender(IServiceProvider serviceProvider, LpBase lpBase)
    {
        this.serviceScope = serviceProvider.CreateScope();
        this.serviceProvider = this.serviceScope.ServiceProvider;
        this.lpBase = lpBase;
    }

    void INetObject.OnConnectionClosed()
    {
        this.serviceScope.Dispose();
        Console.WriteLine("Server IServiceScope Disposed");
    }

    async Task<NetResult> INetServiceWithConnectBidirectionally.ConnectBidirectionally(CertificateToken<ConnectionAgreement>? token)
    {
        var serverConnection = TransmissionContext.Current.ServerConnection;
        if (token is null ||
            !token.ValidateAndVerify(serverConnection) ||
            !token.PublicKey.Equals(this.lpBase.RemotePublicKey))
        {
            return NetResult.NotAuthenticated;
        }

        this.IsAuthenticated = true;
        return NetResult.Success;
    }

    async Task<NetResult> IRemoteUserInterfaceSender.Send(string message)
    {
        if (!this.IsAuthenticated)
        {
            return NetResult.NotAuthenticated;
        }

        this.Prepare();
        _ = this.simpleParser.ParseAndRunAsync(message).ConfigureAwait(false);

        return NetResult.Success;
    }

    [MemberNotNull(nameof(simpleParser))]
    private void Prepare()
    {//
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

        this.serviceProvider.GetRequiredService<UserInterfaceContext>().InitializeRemote(TransmissionContext.Current.ServerConnection);
        this.simpleParser = new SimpleParser(subcommands, subcommandOptions);
    }
}
