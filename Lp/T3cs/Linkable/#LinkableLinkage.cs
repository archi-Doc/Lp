// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Lp.T3cs;

[ValueLinkObject]
[TinyhandObject]
public partial class LinkableLinkage : Linkage
{
    public static bool TryCreate(LinkageEvidence evidence1, LinkageEvidence evidence2, [MaybeNullWhen(false)] out LinkableLinkage? linkage)
        => TryCreate(() => new(), evidence1, evidence2, out linkage);

    [Link(Primary = true, Unique = true, Type = ChainType.Ordered, TargetMember = nameof(LinkedMics), AddValue = false)]
    protected LinkableLinkage()
    {
    }

    public LinkableProof LinkableProof1 => (LinkableProof)this.BaseProof1;

    public LinkableProof LinkableProof2 => (LinkableProof)this.BaseProof2;
}
