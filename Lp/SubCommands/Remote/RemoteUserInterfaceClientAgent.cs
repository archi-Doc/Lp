// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.NetServices;

[NetServiceObject]
public partial class RemoteUserInterfaceClientAgent : IRemoteUserInterfaceClient
{// INetServiceWithUpdateAgreement
    private readonly LpBase lpBase;

    public bool IsAuthenticated { get; private set; }

    public RemoteUserInterfaceClientAgent(LpBase lpBase)
    {
        this.lpBase = lpBase;
    }

    async NetTask<NetResult> INetServiceWithConnectBidirectionally.ConnectBidirectionally(CertificateToken<ConnectionAgreement>? token)
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

        Console.WriteLine(message);

        return NetResult.Success;
    }
}
