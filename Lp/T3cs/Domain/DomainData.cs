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

    public DomainRole Role => this.role;

    public Credit Credit => this.DomainAssignment.CertificateProof.MergedProof.Value.Credit;

    public DomainData(CreditService creditService, ILogger<DomainData> logger, DomainAssignment domainAssignment, SeedKey? domainSeedKey)
    {
        this.Initialize(creditService, logger, domainAssignment, domainSeedKey);
    }

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

    internal async Task MaintainRoot(CancellationToken cancellationToken)
    {
        var credit = this.Credit;//
        var creditIdentity = new CreditIdentity(default, this.domainSeedKey.GetSignaturePublicKey(), credit.Mergers);
        using (var scope = await this.creditService.CreateEquityCredit(creditIdentity))
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
