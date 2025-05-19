// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject]
public partial class LinkLinkage : Linkage
{
    [Link(Primary = true, Unique = true, Type = ChainType.Ordered, TargetMember = "LinkProof1")]
    public LinkLinkage(LinkProof linkProof1, LinkProof linkProof2)
        : base(linkProof1, linkProof2)
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
