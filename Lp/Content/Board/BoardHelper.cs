// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Content;

public static class BoardHelper
{
    public static async Task CreateBoard(Credentials credentials, SignaturePublicKey merger, SignaturePublicKey originator)
    {
        if (!credentials.MergerCredentials.TryGet(merger, out var evidence))
        {
            return;
        }

        var creditIdentity = new CreditIdentity()
        {
            SourceIdentifier = default,
            Originator = originator,
            Mergers = [merger],
            Kind = CreditKind.Board,
        };

        var identifier = creditIdentity.GetIdentifier();

        var creditColor = CreditColor.NewBoard();

    }
}
