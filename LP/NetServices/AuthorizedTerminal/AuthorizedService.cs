// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using Netsphere.Crypto;

namespace Netsphere;

/// <summary>
/// A base interface for authorized service.
/// </summary>
public interface IAuthorizedService : INetService
{
    NetTask<NetResult> Authorize(Token token);

    NetTask<NetResult> Engage(EngageProof proof);

    // bool Engaged { get; }
}

public class AuthorizedService : IAuthorizedService
{
    public async NetTask<NetResult> Authorize(Token token)
    {// -> Engage
        //tempcode
        /*if (CallContext.Current.ServerContext.Terminal.ValidateAndVerifyToken(token) &&
            token.TokenType == Token.Type.Authorize)
        {
            this.AuthorizedKey = token.PublicKey;
            this.Engaged = true;
            return NetResult.Success;
        }*/

        this.Engaged = false;
        return NetResult.NotAuthorized;
    }

    public async NetTask<NetResult> Engage(EngageProof proof)
    {// -> Engage
        //tempcode
        /*if (proof.ValidateAndVerify(CallContext.Current.ServerContext.Terminal.Salt))
        {
            this.AuthorizedKey = proof.PublicKey;
            this.Engaged = true;
            return NetResult.Success;
        }*/

        this.Engaged = false;
        return NetResult.NotAuthorized;
    }

    public SignaturePublicKey AuthorizedKey { get; private set; }

    public bool Engaged { get; private set; }
}
