// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using Arc.Collections;
using Lp.Services;
using Netsphere.Crypto;
using Tinyhand.IO;
using Tinyhand.Tree;
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
    [Link(Type = ChainType.Ordered)]
    public long LinkedMics { get; private set; }

    [Key(Evidence.ReservedKeyCount + 2)]
    public ProofWithLinker LinkageProof1 { get; private set; }

    [Key(Evidence.ReservedKeyCount + 3)]
    public ProofWithLinker LinkageProof2 { get; private set; }

    public override Proof Proof => this.LinkageProof1;

    public LinkageEvidence(long linkedMics, ProofWithLinker linkageProof, ProofWithLinker linkageProof2)
    {
        this.LinkedMics = linkedMics;
        this.LinkageProof1 = linkageProof;
        this.LinkageProof2 = linkageProof2;
        this.SetIdentifier();
    }

    private void SetIdentifier()
    {
        TinyhandWriter writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        try
        {
            writer.Write(this.LinkedMics);
            TinyhandSerializer.SerializeObject(ref writer, this.LinkageProof1, TinyhandSerializerOptions.Signature);
            TinyhandSerializer.SerializeObject(ref writer, this.LinkageProof2, TinyhandSerializerOptions.Signature);
            writer.FlushAndGetReadOnlySpan(out var span, out var _);
            this.Identifier = new Identifier(Blake3.Get256_UInt64(span));
        }
        finally
        {
            writer.Dispose();
        }
    }
}
