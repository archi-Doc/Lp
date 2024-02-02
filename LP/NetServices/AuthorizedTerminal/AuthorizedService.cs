// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using Netsphere.Crypto;
using Netsphere.Server;

namespace Netsphere;

/// <summary>
/// A base interface for authorized service.
/// </summary>
public interface IAuthorizedService : INetService
{
    NetTask<NetResult> Authenticate(AuthenticationToken token);

    // NetTask<NetResult> Engage(EngageProof proof);

    // bool Engaged { get; }
}

public class AuthorizedService : IAuthorizedService
{
    public async NetTask<NetResult> Authenticate(AuthenticationToken token)
    {
        if (TransmissionContext.Current.Connection.ValidateAndVerify(token))
        {
            this.AuthenticationKey = token.PublicKey;
            this.Authenticated = true;
            return NetResult.Success;
        }

        this.Authenticated = false;
        return NetResult.NotAuthorized;
    }

    /*public async NetTask<NetResult> Engage(EngageProof proof)
    {// -> Engage
        /*if (proof.ValidateAndVerify(CallContext.Current.ServerContext.Terminal.Salt))
        {
            this.AuthorizedKey = proof.PublicKey;
            this.Engaged = true;
            return NetResult.Success;
        }

        this.Engaged = false;
        return NetResult.NotAuthorized;
    }*/

    public SignaturePublicKey AuthenticationKey { get; private set; }

    public bool Authenticated { get; private set; }
}
