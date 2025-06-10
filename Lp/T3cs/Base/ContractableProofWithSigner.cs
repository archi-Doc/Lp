// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

/// <summary>
/// The general Proof class only supports authentication using the target <see cref="SignaturePublicKey"/>,<br/>
/// but this class supports authentication using the target PublicKey, Mergers, and LpKey.<br/>
/// The authentication key is determined as follows: <br/>
/// If Signer is 0, the target PublicKey is used;<br/>
/// if it is between 1 and MergerCount, a Merger is used;<br/>
/// otherwise, LpKey is used.
/// </summary>
public abstract partial class ContractableProofWithSigner : ContractableProof
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = ContractableProof.ReservedKeyCount + 2;

    #region FieldAndProperty

    public abstract PermittedSigner PermittedSigner { get; }

    [Key(ContractableProof.ReservedKeyCount + 0)]
    public Value Value { get; protected set; }

    /// <summary>
    /// Gets or sets the signer index indicating which key is used for authentication.<br/>
    /// If <c>0</c>, the target <see cref="Value.Owner"/> is used.<br/>
    /// If between <c>1</c> and <c>Value.Credit.MergerCount</c>, a merger key is used.<br/>
    /// Otherwise, the <see cref="LpConstants.LpPublicKey"/> is used.
    /// </summary>
    [Key(ContractableProof.ReservedKeyCount + 1)]
    public int Signer { get; protected set; }

    #endregion

    public ContractableProofWithSigner(SignaturePublicKey linkerPublicKey, Value value)
        : base(linkerPublicKey)
    {
        this.Value = value;
    }

    public override SignaturePublicKey GetSignatureKey()
    {
        if (this.Signer == 0)
        {
            return this.Value.Owner;
        }
        else if (this.Signer > 0 && this.Signer <= this.Value.Credit.MergerCount)
        {
            return this.Value.Credit.Mergers[this.Signer - 1];
        }

        return LpConstants.LpPublicKey;
    }

    public override bool TryGetCredit([MaybeNullWhen(false)] out Credit credit)
    {
        credit = this.Value.Credit;
        return true;
    }

    /*public override bool TryGetValue([MaybeNullWhen(false)] out Value value)
    {
        value = this.Value;
        return true;
    }*/

    public override bool Validate()
    {
        if (!base.Validate())
        {
            return false;
        }

        if (!this.Value.Validate())
        {
            return false;
        }

        if (this.Signer == 0)
        {
            return this.PermittedSigner.HasFlag(PermittedSigner.Owner);
        }
        else if (this.Signer > 0 && this.Signer <= LpConstants.MaxMergers)
        {
            return this.PermittedSigner.HasFlag(PermittedSigner.Merger);
        }
        else
        {
            return this.PermittedSigner.HasFlag(PermittedSigner.LpKey);
        }
    }

    public override bool PrepareForSigning(ref SignaturePublicKey publicKey, long validMics)
    {
        var permittedSigner = this.PermittedSigner;
        var mergerCount = this.Value.Credit.Mergers.Length;

        if (permittedSigner.HasFlag(PermittedSigner.Owner) &&
            this.Value.Owner.Equals(ref publicKey))
        {// Owner
            this.Signer = 0;
            goto Success;
        }

        if (permittedSigner.HasFlag(PermittedSigner.LpKey) &&
                LpConstants.LpPublicKey.Equals(ref publicKey))
        {// LpKey
            this.Signer = -1;
            goto Success;
        }

        if (permittedSigner.HasFlag(PermittedSigner.Merger))
        {// Mergers
            if (mergerCount == 0)
            {
                return false;
            }
            else if (this.Value.Credit.Mergers[0].Equals(ref publicKey))
            {// Merger-0
                this.Signer = 1;
                goto Success;
            }

            if (mergerCount == 1)
            {
                return false;
            }
            else if (this.Value.Credit.Mergers[1].Equals(ref publicKey))
            {// Merger-1
                this.Signer = 2;
                goto Success;
            }

            if (this.Value.Credit.Mergers[2].Equals(ref publicKey))
            {// Merger-2
                this.Signer = 3;
                goto Success;
            }
        }

        return false;

Success:
        base.PrepareForSigning(ref publicKey, validMics);
        return true;
    }
}
