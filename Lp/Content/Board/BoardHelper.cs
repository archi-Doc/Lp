// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Content;

public static class BoardHelper
{
    public static async Task CreateBoard(SignaturePublicKey merger, SignaturePublicKey originator)
    {
        var creditIdentity = new CreditIdentity()
        {
            SourceIdentifier = default,
        };

        var identifier = creditIdentity.GetIdentifier();
    }
}
