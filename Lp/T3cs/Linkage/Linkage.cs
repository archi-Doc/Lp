// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject]
public partial class Linkage : IValidatable
{
    #region FieldAndProperty

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    public long LinkedMics { get; private set; }

    [Key(1)]
    public ProofWithLinker LinkageProof1 { get; private set; }

    [Key(2)]
    public ProofWithLinker LinkageProof2 { get; private set; }

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

    public Linkage(ProofWithLinker proof1, ProofWithLinker proof2)
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
}

/*[TinyhandObject]
[ValueLinkObject]
public partial class Linkage : IValidatable
{
    #region FieldAndProperty

    [Key(0)]
    public Evidence Evidence1 { get; set; }

    [Key(1)]
    public Evidence Evidence2 { get; set; }

    [Key(2)]
    private byte[]? linkerSignature;

    #endregion

    public Linkage(Evidence evidence1, Evidence evidence2)
    {
        this.Evidence1 = evidence1;
        this.Evidence2 = evidence2;
    }

    public bool Validate()
    {
        if (!this.Evidence1.ValidateLinker() ||
            !this.Evidence2.ValidateLinker())
        {
            return false;
        }

        if (!this.Evidence1.Proof.Equals(this.Evidence2.Proof))
        {
            return false;
        }

        return true;
    }
}*/
