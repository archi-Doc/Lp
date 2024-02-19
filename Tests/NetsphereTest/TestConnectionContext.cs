// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Server;

namespace NetsphereTest;

public class TestConnectionContext : ServerConnectionContext
{
    public TestConnectionContext(ServerConnection serverConnection)
        : base(serverConnection)
    {
    }

    public override NetResult RespondUpdateAgreement(CertificateToken<ConnectionAgreement> token)
    {// Accept all agreement.
        if (!this.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            return NetResult.NotAuthorized;
        }

        return NetResult.Success;
    }

    public override NetResult RespondConnectBidirectionally(CertificateToken<ConnectionAgreement>? token)
    {// Enable bidirectional connection.
        if (token is null ||
            !this.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            return NetResult.NotAuthorized;
        }

        return NetResult.Success;
    }
}
