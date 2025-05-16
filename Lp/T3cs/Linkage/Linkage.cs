// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;
using Tinyhand.IO;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject]
public partial class Linkage : IValidatable
{
    private static readonly ObjectPool<LinkageEvidence> EvidencePool = new(() => LinkageEvidence.UnsafeConstructor());

    #region FieldAndProperty

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    public long LinkedMics { get; private set; }

    [Key(1)]
    public LinkageProof LinkageProof1 { get; private set; }

    [Key(2)]
    public LinkageProof LinkageProof2 { get; private set; }

    [Key(3, Level = TinyhandWriter.DefaultSignatureLevel + 10)]
    private byte[]? linkerSignature;

    [Key(4, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    public byte[]? MergerSignature10 { get; private set; }

    [Key(5, Level = TinyhandWriter.DefaultSignatureLevel + 2)]
    public byte[]? MergerSignature11 { get; private set; }

    [Key(6, Level = TinyhandWriter.DefaultSignatureLevel + 3)]
    public byte[]? MergerSignature12 { get; private set; }

    [Key(7, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    public byte[]? MergerSignature20 { get; private set; }

    [Key(8, Level = TinyhandWriter.DefaultSignatureLevel + 2)]
    public byte[]? MergerSignature21 { get; private set; }

    [Key(9, Level = TinyhandWriter.DefaultSignatureLevel + 3)]
    public byte[]? MergerSignature22 { get; private set; }

    #endregion

    public Linkage(LinkageProof proof1, LinkageProof proof2)
    {
        this.LinkageProof1 = proof1;
        this.LinkageProof2 = proof2;
    }

    public bool Validate()
    {
        /*if (!this.LinkageProof1.ValidateLinker() ||
            !this.LinkageProof2.ValidateLinker())
        {
            return false;
        }*/

        return true;
    }

    public bool ValidateAndVerify()
    {
        if (!this.Validate())
        {
            return false;
        }

        var evidence = EvidencePool.Rent();
        try
        {
            evidence.FromLinkage(this, true);
            if (!evidence.ValidateAndVerify())
            {
                return false;
            }

            evidence.FromLinkage(this, false);
            if (!evidence.ValidateAndVerify())
            {
                return false;
            }
        }
        finally
        {
            EvidencePool.Return(evidence);
        }

        return true;
    }
}
