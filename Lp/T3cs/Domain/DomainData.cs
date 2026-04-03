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

    private ILogger logger;
    private DomainRole role;
    private SeedKey? domainSeedKey;

    public DomainRole Role => this.role;

    public DomainData(ILogger<DomainData> logger, DomainAssignment domainAssignment, SeedKey? domainSeedKey)
    {
        this.Initialize(logger, domainAssignment, domainSeedKey);
    }

    [MemberNotNull(nameof(logger))]
    [MemberNotNull(nameof(DomainAssignment))]
    public void Initialize(ILogger logger, DomainAssignment domainAssignment, SeedKey? domainSeedKey)
    {
        this.logger = logger;
        this.DomainAssignment = domainAssignment;
        this.domainSeedKey = domainSeedKey;
    }

    public override string ToString()
    {
        return $"{this.Role} {this.DomainAssignment?.ToString()}";
    }

    internal void DetermineRole()
    {
        if (this.domainSeedKey is not null)
        {
            var originator = this.domainSeedKey.GetSignaturePublicKey();
            if (this.DomainAssignment.CertificateProof.MergedProof.Value.Credit.PrimaryMerger.Equals(ref originator))
            {// Root
                this.role = DomainRole.Root;
                this.logger.GetWriter()?.Write(Hashed.Domain.RootAssigned, this.DomainAssignment.Name);
                return;
            }
        }

        // Proof -> Peer

        // Scout

    }

    internal async Task<CertificateProof?> ExchangeProof(CertificateProof? proof)
    {
        return default;
    }

    internal void RadiateProof(CertificateProof proof, ref ResponseChannel<int> channel)
    {
    }

    internal async Task<(bool IsPeer, CertificateProof? NewProof)> Scout(CertificateToken<SignaturePublicKey> token)
    {
        var ownerPublicKey = token.PublicKey;

        /*FullCredit? fullCredit = default;
        fullCredit.Owners.TryLock()
        var owners = await fullCredit.Owners.TryGet();
        var ownerData = owners.TryGet(ownerPublicKey);
        //ownerData.*/

        return default;
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
