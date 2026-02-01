// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class DomainData
{
    [Key(0)]
    private Credit domainCredit;

    [Key(1)]
    private PeerProof.GoshujinClass peerProofs = new();

    private DomainRole role;
    private SeedKey? domainSeedKey;

    public DomainRole Role => this.role;

    public DomainData(Credit domainCredit)
    {
        this.domainCredit = domainCredit;
    }

    public void Update(DomainRole domainRole, SeedKey? domainSeedKey)
    {//
        if (domainRole == DomainRole.Root)
        {
            if (domainSeedKey?.GetSignaturePublicKey().Equals(this.domainCredit.PrimaryMerger) != true)
            {
                domainRole = DomainRole.User;
                domainSeedKey = default;
            }
        }

        this.role = domainRole;
        this.domainSeedKey = domainSeedKey;
    }

    /*public DomainOverview GetOverview()
    {
        int count;
        PeerProof? peerProof;
        using (this.peerProofs.LockObject.EnterScope())
        {
            count = this.peerProofs.Count;
            peerProof = this.peerProofs.GetRandomInternal();
        }

        return new(count, peerProof);
    }*/
}
