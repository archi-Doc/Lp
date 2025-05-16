// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

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
    public LinkageProof LinkageProof1 { get; private set; }

    [Key(Evidence.ReservedKeyCount + 3)]
    public LinkageProof LinkageProof2 { get; private set; }

    public override Proof Proof => this.LinkageProof1;

    #endregion

    public LinkageEvidence(long linkedMics, LinkageProof linkageProof, LinkageProof linkageProof2)
    {
        this.LinkedMics = linkedMics;
        this.LinkageProof1 = linkageProof;
        this.LinkageProof2 = linkageProof2;
        this.SetIdentifier();
    }

    internal void FromLinkage(Linkage linkage, bool first)
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
