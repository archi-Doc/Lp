// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[NetService]
public partial interface ILinkerToMerger : INetServiceWithAuthenticate
{
}

[NetObject]
internal class LinkerToMergerAgent : ILinkerToMerger
{
    private readonly Merger merger;

    public LinkerToMergerAgent(Merger merger)
    {
        this.merger = merger;
    }

    public async Task<NetResult> Authenticate(AuthenticationToken token)
    {
        var serverConnection = TransmissionContext.Current.ServerConnection;
        if (!token.ValidateAndVerify(serverConnection))
        {
            return NetResult.NotAuthenticated;
        }

        throw new NotImplementedException();
    }
}
