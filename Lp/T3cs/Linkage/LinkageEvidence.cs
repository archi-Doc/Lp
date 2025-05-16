// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Collections;
using Lp.Services;
using Netsphere.Crypto;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Integrality = false, Isolation = IsolationLevel.None)]
public partial class LinkageEvidence : Evidence
{
    [Key(Evidence.ReservedKeyCount + 0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    public Identifier Identifier { get; private set; }

    [Key(Evidence.ReservedKeyCount + 1)]
    public ProofWithLinker LinkageProof1 { get; private set; }

    [Key(Evidence.ReservedKeyCount + 2)]
    public ProofWithLinker LinkageProof2 { get; private set; }

    public override Proof Proof => this.LinkageProof1;

    public LinkageEvidence(Identifier identifier, ProofWithLinker linkageProof, ProofWithLinker linkageProof2)
    {
        this.Identifier = identifier;//
        this.LinkageProof1 = linkageProof;
        this.LinkageProof2 = linkageProof2;
    }
}
