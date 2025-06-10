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

    //public abstract SignaturePublicKey GetSignatureKey();

    /// <summary>
    /// Tries to get the linker public key associated with the proof.
    /// </summary>
    /// <param name="linkerPublicKey"> When this method returns, contains the linker public key if available; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if the linker public key is available; otherwise, <c>false</c>.</returns>
    public virtual bool TryGetLinkerPublicKey(out SignaturePublicKey linkerPublicKey)
    {
        linkerPublicKey = this.LinkerPublicKey;
        return true;
    }
}
