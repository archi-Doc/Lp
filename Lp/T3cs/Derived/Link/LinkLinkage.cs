// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject]
public partial class LinkLinkage : Linkage
{
    public static bool TryCreate(LinkageEvidence evidence1, LinkageEvidence evidence2, [MaybeNullWhen(false)] out LinkLinkage? linkage)
        => TryCreate(() => new LinkLinkage(), evidence1, evidence2, out linkage);

    [Link(Primary = true, Unique = true, Type = ChainType.Ordered, TargetMember = "LinkProof1")]
    protected LinkLinkage()
        : base()
    {
    }

    public LinkProof LinkProof1 => (LinkProof)this.BaseProof1;

    public LinkProof LinkProof2 => (LinkProof)this.BaseProof2;
}

/*[TinyhandObject]
public partial class LinkEvidence : Evidence
{
    [Key(Evidence.ReservedKeyCount)]
    public LinkProof LinkProof { get; private set; }

    public override Proof BaseProof => this.LinkProof;

    public LinkEvidence(LinkProof linkProof)
    {
        this.LinkProof = linkProof;
    }
}*/
