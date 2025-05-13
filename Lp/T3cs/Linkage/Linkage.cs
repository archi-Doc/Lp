// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public sealed partial class LinkProof : ProofAndValue
{
    [Key(Proof.ReservedKeyCount)]
    public SignaturePublicKey LinkerPublicKey { get; private set; }

    public LinkProof()
    {
    }

    public LinkProof(SignaturePublicKey linkerPublicKey, Value value)
    {
        this.LinkerPublicKey = linkerPublicKey;
        this.Value = value;
    }

    public override bool TryGetLinkerPublicKey([MaybeNullWhen(false)] out SignaturePublicKey linkerPublicKey)
    {
        linkerPublicKey = this.LinkerPublicKey;
        return true;
    }

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        return true;
    }
}

/*[TinyhandObject]
public partial class LinkerLinkage : Linkage<LinkerProof>
{
}*/

[TinyhandObject]
[ValueLinkObject]
public partial class Linkage : IValidatable
{
    #region FieldAndProperty

    [Key(0)]
    public Proof ProofA { get; set; }

    [Key(1)]
    public Proof ProofB { get; set; }

    [Key(2)]
    private byte[]? mergerSignatureA1;

    [Key(3)]
    private byte[]? mergerSignatureA2;

    [Key(4)]
    private byte[]? mergerSignatureA3;

    [Key(5)]
    private byte[]? mergerSignatureB1;

    [Key(6)]
    private byte[]? mergerSignatureB2;

    [Key(7)]
    private byte[]? mergerSignatureB3;

    [Key(8)]
    private byte[]? linkerSignature;

    #endregion

    public Linkage(Proof proofA, Proof proofB)
    {
        this.ProofA = proofA;
        this.ProofB = proofB;
    }

    public bool Validate()
    {
        SignaturePublicKey linkerPublicKey;
        if (!this.ProofA.Validate() ||
            !this.ProofA.TryGetLinkerPublicKey(out linkerPublicKey) ||
            !linkerPublicKey.IsValid)
        {
            return false;
        }

        if (!this.ProofB.Validate() ||
            !this.ProofB.TryGetLinkerPublicKey(out linkerPublicKey) ||
            !linkerPublicKey.IsValid)
        {
            return false;
        }

        return true;
    }
}
