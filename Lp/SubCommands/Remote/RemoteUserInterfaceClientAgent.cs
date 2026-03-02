// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.Services;
using Microsoft.Extensions.DependencyInjection;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.NetServices;

[NetServiceObject]
public partial class RemoteUserInterfaceClientAgent : IRemoteUserInterfaceClient
{// INetServiceWithUpdateAgreement
    private readonly IServiceScope serviceScope;
    private readonly IServiceProvider serviceProvider;
    private readonly LpBase lpBase;
    private SimpleParser? simpleParser;

    public bool IsAuthenticated { get; private set; }

    public RemoteUserInterfaceClientAgent(IServiceProvider serviceProvider, LpBase lpBase)
    {
        this.serviceScope = serviceProvider.CreateScope();// Release
        this.serviceProvider = this.serviceScope.ServiceProvider;
        this.lpBase = lpBase;
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

    async Task<NetResult> IRemoteUserInterfaceClient.Send(string message)
    {
        if (!this.IsAuthenticated)
        {
            return NetResult.NotAuthenticated;
        }

        if (!this.Prepare())
        {
            return NetResult.UnknownError;
        }

        _ = this.simpleParser.ParseAndRunAsync(message).ConfigureAwait(false);

        Console.WriteLine(message);

        return NetResult.Success;
    }

    [MemberNotNull(nameof(simpleParser))]
    private bool Prepare()
    {
        if (this.simpleParser is not null)
        {
            return;
        }


        this.serviceProvider.GetRequiredService<UserInterfaceContext>().InitializeRemote(TransmissionContext.Current.ServerConnection);

        var subcommandOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = this.serviceProvider,
            RequireStrictCommandName = true,
            RequireStrictOptionName = true,
            DoNotDisplayUsage = true,
            DisplayCommandListAsHelp = true,
            AutoAlias = true,
        };

        this.simpleParser = new SimpleParser(context.Subcommands, subcommandOptions);

        return true;
    }
}
