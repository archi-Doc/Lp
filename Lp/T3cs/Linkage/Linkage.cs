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
}
