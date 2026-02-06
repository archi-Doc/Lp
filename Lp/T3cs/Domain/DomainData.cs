// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class DomainData
{
    [Key(0)]
    private DomainAssignment domainAssignment;

    [Key(1)]
    private PeerProof.GoshujinClass peerProofs = new();

    private DomainRole role;
    private SeedKey? domainSeedKey;

    public DomainRole Role => this.role;

    public DomainData(DomainAssignment domainAssignment, SeedKey? domainSeedKey)
    {
        this.Initialize(domainAssignment, domainSeedKey);
    }

    [MemberNotNull(nameof(domainAssignment))]
    public void Initialize(DomainAssignment domainAssignment, SeedKey? domainSeedKey)
    {
        this.domainAssignment = domainAssignment;
        this.domainSeedKey = domainSeedKey;
    }

    public override string ToString()
    {
        return $"{this.Role} {this.domainAssignment?.ToString()}";
    }

    internal void Initial()
    {
        if (this.role == DomainRole.User)
        {

        }
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
