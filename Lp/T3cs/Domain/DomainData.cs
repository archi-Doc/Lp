// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class DomainData
{
    [Key(0)]
    public DomainAssignment DomainAssignment { get; private set; }

    [Key(1)]
    private PeerProof.GoshujinClass peerProofs = new();

    private DomainRole role;
    private SeedKey? domainSeedKey;

    public DomainRole Role => this.role;

    public DomainData(DomainAssignment domainAssignment, SeedKey? domainSeedKey)
    {
        this.Initialize(domainAssignment, domainSeedKey);
    }

    [MemberNotNull(nameof(DomainAssignment))]
    public void Initialize(DomainAssignment domainAssignment, SeedKey? domainSeedKey)
    {
        this.DomainAssignment = domainAssignment;
        this.domainSeedKey = domainSeedKey;
    }

    public override string ToString()
    {
        return $"{this.Role} {this.DomainAssignment?.ToString()}";
    }

    internal void Initial()
    {
        if (this.role == DomainRole.User)
        {
            if (this.domainSeedKey is not null)
            {
                var originator = this.domainSeedKey.GetSignaturePublicKey();
                var creditIdentity = new CreditIdentity(default, originator, this.DomainAssignment.Credit.Mergers);
                if (this.DomainAssignment.Credit.Identifier.Equals(creditIdentity.GetIdentifier()))
                {
                    this.role = DomainRole.Root;
                }
            }
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
