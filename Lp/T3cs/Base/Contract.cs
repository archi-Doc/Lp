// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public readonly partial struct Contract : IEquatable<Contract>
{
    [Key(0)]
    public readonly Proof Proof;

    [Key(1)]
    public readonly Point Partial;

    [Key(2)]
    public readonly Point Total;

    public Contract(LinkableProof proof, Point partial, Point total)
    {
        this.Proof = proof;
        this.Partial = partial;
        this.Total = total;
    }

    public Contract(LinkableProof proof)
    {
        this.Proof = proof;
    }

    public bool Equals(Contract other)
        => this.Proof.Equals(other.Proof) && this.Partial == other.Partial && this.Total == other.Total;
}

/*[ValueLinkObject]
public partial class ContractedLinkage : Linkage
{
    public static bool TryCreate(LinkableEvidence evidence1, LinkableEvidence evidence2, [MaybeNullWhen(false)] out MarketableLinkage linkage)
        => TryCreate(() => new(), evidence1, evidence2, out linkage);

    [Link(Primary = true, Unique = true, Type = ChainType.Ordered, TargetMember = nameof(LinkedMics))]
    protected ContractedLinkage()
    {
    }

    public MarketableProof LinkableProof1 => (MarketableProof)this.BaseProof1;

    public MarketableProof LinkableProof2 => (MarketableProof)this.BaseProof2;
}*/
