// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using FastExpressionCompiler;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.Net;

internal class DomainServiceClass
{
    public enum Kind : byte
    {
        Root,
        Leaf,
        External,
    }

    private readonly Credit domainCredit;
    private SeedKey? domainSeedKey;
    private PeerProof.GoshujinClass peerProofs = new();

    public DomainServiceClass(Credit domainCredit)
    {
        this.domainCredit = domainCredit;
    }

    public DomainOverview GetOverview()
    {
        int count;
        PeerProof? peerProof;
        using (this.peerProofs.LockObject.EnterScope())
        {
            count = this.peerProofs.Count;
            peerProof = this.peerProofs.GetRandomInternal();
        }

        return new(count, peerProof);
    }
}
