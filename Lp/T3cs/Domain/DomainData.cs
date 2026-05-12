// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.Services;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class DomainData
{
    [Key(0)]
    public DomainAssignment DomainAssignment { get; private set; }

    [Key(1)]
    private PeerProof.GoshujinClass peerProofs = new();

    private CreditService creditService;
    private ILogger logger;
    private DomainRole role;
    private SeedKey? domainSeedKey;

    // Root
    private EquityCredit? equityCredit;

    public DomainRole Role => this.role;

    public Credit Credit => this.DomainAssignment.CertificateProof.MergedProof.Value.Credit;

    public DomainData(CreditService creditService, ILogger<DomainData> logger, DomainAssignment domainAssignment, SeedKey? domainSeedKey)
    {
        this.Initialize(creditService, logger, domainAssignment, domainSeedKey);
    }

    [MemberNotNull(nameof(creditService))]
    [MemberNotNull(nameof(logger))]
    [MemberNotNull(nameof(DomainAssignment))]
    public void Initialize(CreditService creditService, ILogger logger, DomainAssignment domainAssignment, SeedKey? domainSeedKey)
    {
        this.creditService = creditService;
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

    internal async Task MaintainRoot(CancellationToken cancellationToken)
    {
        var creditIdentity = new CreditIdentity(default, this.domainSeedKey.GetSignaturePublicKey(), this.Credit.Mergers);
        using (var scope = await this.creditService.CreateEquityCredit(creditIdentity))
        {
            if (scope.IsValid)
            {
                this.equityCredit = scope.Data;
            }
        }
    }

    internal async Task<(bool IsPeer, MergedProof? NewProof)> Scout(CertificateToken<SignaturePublicKey>? token)
    {
        MergedProof? proof = default;
        bool isPeer = false;
        if (token is not null &&
            this.Role == DomainRole.Root &&
            this.equityCredit is not null &&
            this.domainSeedKey is not null)
        {
            var ownerData = await this.equityCredit.GetOwnerData(token.PublicKey).ConfigureAwait(false);
            if (ownerData is not null)
            {
                proof = new(new(ownerData.PublicKey, ownerData.Point, this.equityCredit.Credit));
                if (!this.domainSeedKey.TrySignAndValidate(proof, 10))
                {
                    proof = default;
                }
            }
        }

        /*FullCredit? fullCredit = default;
        fullCredit.Owners.TryLock()
        var owners = await fullCredit.Owners.TryGet();
        var ownerData = owners.TryGet(ownerPublicKey);
        //ownerData.*/

        return (isPeer, proof);
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
