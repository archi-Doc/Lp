// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
// [ValueLinkObject(Integrality = true, Isolation = IsolationLevel.Serializable)]
public partial class LinkEvidence : Evidence
{
    [Key(Evidence.ReservedKeyCount)]
    public LinkProof LinkProof { get; private set; }

    public override Proof BaseProof => this.LinkProof;

    public LinkEvidence(LinkProof linkProof)
    {
        this.LinkProof = linkProof;
    }
}
