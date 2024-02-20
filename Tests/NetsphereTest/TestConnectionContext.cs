﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace NetsphereTest;

public class TestConnectionContext : ServerConnectionContext
{
    public TestConnectionContext(ServerConnection serverConnection)
        : base(serverConnection)
    {
    }

    public override bool RespondUpdateAgreement(CertificateToken<ConnectionAgreement> token)
    {// Accept all agreement.
        if (!this.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            return false;
        }

        return true;
    }

    public override bool RespondConnectBidirectionally(CertificateToken<ConnectionAgreement>? token)
    {// Enable bidirectional connection.
        if (token is null ||
            !this.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            return false;
        }

        return true;
    }
}
