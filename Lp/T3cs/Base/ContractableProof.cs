// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

public abstract partial class ContractableProof : ProofWithSigner
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = ProofWithSigner.ReservedKeyCount + 1;

    #region FieldAndProperty

    [Key(ProofWithSigner.ReservedKeyCount + 0)]
    public SignaturePublicKey LinkerPublicKey { get; private set; }

    #endregion

    public ContractableProof(Value value, SignaturePublicKey linkerPublicKey)
        : base(value)
    {
        this.LinkerPublicKey = linkerPublicKey;
    }

    public override bool TryGetLinkerPublicKey(out SignaturePublicKey linkerPublicKey)
    {
        linkerPublicKey = this.LinkerPublicKey;
        return true;
    }
}
