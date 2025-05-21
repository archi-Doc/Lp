// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Lp.T3cs;

[ValueLinkObject]
[TinyhandObject]
public partial class MarketableLinkage : Linkage
{
    public static bool TryCreate(LinkableEvidence evidence1, LinkableEvidence evidence2, [MaybeNullWhen(false)] out MarketableLinkage linkage)
        => TryCreate(() => new(), evidence1, evidence2, out linkage);

    [Link(Primary = true, Unique = true, Type = ChainType.Ordered, TargetMember = nameof(LinkedMics), AddValue = false)]
    protected MarketableLinkage()
    {
    }

    public MarketableProof LinkableProof1 => (MarketableProof)this.BaseProof1;

    public MarketableProof LinkableProof2 => (MarketableProof)this.BaseProof2;
}
