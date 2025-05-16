// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Security.Policy;
using Arc.Collections;
using Lp.Services;
using Netsphere.Crypto;
using Tinyhand.IO;
using Tinyhand.Tree;
using ValueLink.Integrality;

namespace Lp.T3cs;

[TinyhandObject]
public readonly partial struct LinkageEvidenceStruct
{
    [Key(1, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    public readonly byte[]? MergerSignature0;

    [Key(2, Level = TinyhandWriter.DefaultSignatureLevel + 2)]
    public readonly byte[]? MergerSignature1;

    [Key(3, Level = TinyhandWriter.DefaultSignatureLevel + 3)]
    public readonly byte[]? MergerSignature2;

    [Key(Evidence.ReservedKeyCount + 1)]
    public readonly long LinkedMics;

    [Key(Evidence.ReservedKeyCount + 2)]
    public readonly ProofWithLinker LinkageProof1;

    [Key(Evidence.ReservedKeyCount + 3)]
    public readonly ProofWithLinker LinkageProof2;

    public LinkageEvidenceStruct(Linkage linkage, bool first)
    {
        this.LinkedMics = linkage.LinkedMics;
        this.LinkageProof1 = linkage.LinkageProof1;
        this.LinkageProof2 = linkage.LinkageProof2;

        if (first)
        {
            this.MergerSignature0 = linkage.MergerSignature10;
            this.MergerSignature1 = linkage.MergerSignature11;
            this.MergerSignature2 = linkage.MergerSignature12;
        }
        else
        {
            this.MergerSignature0 = linkage.MergerSignature20;
            this.MergerSignature1 = linkage.MergerSignature21;
            this.MergerSignature2 = linkage.MergerSignature22;
        }
    }
}

[TinyhandObject]
[ValueLinkObject(Integrality = false, Isolation = IsolationLevel.None)]
public partial class LinkageEvidence : Evidence
{
    #region FieldAndProperty

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

    #endregion

    public static LinkageEvidence FromLinkage(Linkage linkage, bool first)
    {
        var evidence = new LinkageEvidence(linkage.LinkedMics, linkage.LinkageProof1, linkage.LinkageProof2);
        if (first)
        {
            evidence.MergerSignature0 = linkage.MergerSignature10;
            evidence.MergerSignature1 = linkage.MergerSignature11;
            evidence.MergerSignature2 = linkage.MergerSignature12;
        }
        else
        {
            evidence.MergerSignature0 = linkage.MergerSignature20;
            evidence.MergerSignature1 = linkage.MergerSignature21;
            evidence.MergerSignature2 = linkage.MergerSignature22;
        }

        return evidence;
    }

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
