// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Services;

public class LpBoardService(Credentials credentials)
{
    private readonly Credentials credentials = credentials;

    public async Task CreateBoard(SignaturePublicKey merger, SignaturePublicKey originator)
    {
        if (!this.credentials.Nodes.TryGet(merger, out var evidence))
        {
            return;
        }

        var creditIdentity = new CreditIdentity(default, originator, [merger]);

        var identifier = creditIdentity.GetIdentifier();

        var creditColor = CreditColor.NewBoard();
    }
}
