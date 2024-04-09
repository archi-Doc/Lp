// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace LP.NetServices;

/// <summary>
/// A base interface for authentication.
/// </summary>
public interface IAuthenticationService : INetService
{
    NetTask<NetResult> Authenticate(AuthenticationToken token);
}

internal class AuthorizedServiceAgent : IAuthenticationService
{
    public async NetTask<NetResult> Authenticate(AuthenticationToken token)
    {
        if (TransmissionContext.Current.ServerConnection.ValidateAndVerifyWithSalt(token))
        {
            this.AuthenticationKey = token.PublicKey;
            this.Authenticated = true;
            return NetResult.Success;
        }

        this.Authenticated = false;
        return NetResult.NotAuthorized;
    }

    public SignaturePublicKey AuthenticationKey { get; private set; }

    public bool Authenticated { get; private set; }
}
