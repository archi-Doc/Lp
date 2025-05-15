// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

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
