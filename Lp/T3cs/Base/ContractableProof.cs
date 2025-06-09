// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

public abstract partial class ContractableProof : Proof
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = Proof.ReservedKeyCount + 1;

    #region FieldAndProperty

    [Key(Proof.ReservedKeyCount + 0)]
    public SignaturePublicKey LinkerPublicKey { get; protected set; }

    #endregion

    public ContractableProof(SignaturePublicKey linkerPublicKey)
    {
        this.LinkerPublicKey = linkerPublicKey;
    }

    public override bool TryGetLinkerPublicKey(out SignaturePublicKey linkerPublicKey)
    {
        linkerPublicKey = this.LinkerPublicKey;
        return true;
    }
}
