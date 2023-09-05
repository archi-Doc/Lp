// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;

namespace Netsphere;

/// <summary>
/// A base interface for authorized service.
/// </summary>
public interface IAuthorizedService : INetService
{
    NetTask<NetResult> Authorize(Token token);
}

public class AuthorizedService : IAuthorizedService
{
    public async NetTask<NetResult> Authorize(Token token)
    {// -> Engage
        if (CallContext.Current.ServerContext.Terminal.ValidateAndVerifyToken(token) &&
            token.TokenType == Token.Type.Authorize)
        {
            this.AuthorizedKey = token.PublicKey;
            return NetResult.Success;
        }

        return NetResult.NotAuthorized;
    }

    public PublicKey AuthorizedKey { get; private set; }
}
