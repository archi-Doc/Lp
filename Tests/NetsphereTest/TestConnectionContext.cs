// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace NetsphereTest;

public class TestConnectionContext : ServerConnectionContext
{
    public enum State
    {
        Waiting,
        Running,
        Complete,
    }

    public TestConnectionContext(ServerConnection serverConnection)
        : base(serverConnection)
    {
    }

    public State CurrentState { get; set; }

    public CancellationToken CancellationToken => cts.Token;

    private readonly CancellationTokenSource cts = new();

    public void Terminate()
        => this.cts.Cancel();

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
