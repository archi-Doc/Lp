// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Services;

public class LpBoardService(Credentials credentials)
{
    private readonly Credentials credentials = credentials;

    public async Task CreateBoard(IUserInterfaceService userInterfaceService, SignaturePublicKey merger, SignaturePublicKey originator)
    {
        userInterfaceService.WriteLine("3");
        if (!this.credentials.Nodes.TryGet(merger, out var evidence))
        {
            userInterfaceService.WriteLine("4");
            return;
        }

        userInterfaceService.WriteLine("5");
        var creditIdentity = new CreditIdentity(default, originator, [merger]);

        userInterfaceService.WriteLine("6");
        var identifier = creditIdentity.GetIdentifier();

        userInterfaceService.WriteLine("7");
        var creditColor = CreditColor.NewBoard();
    }
}
