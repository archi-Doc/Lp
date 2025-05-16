// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial class TestLinkageProof : LinkageProof
{
    public TestLinkageProof(Value value, SignaturePublicKey linkerPublicKey)
        : base(value, linkerPublicKey)
    {
    }

    public override PermittedSigner PermittedSigner => PermittedSigner.Owner;

    public override bool Validate()
    {
        return true;
    }
}

/// <summary>
/// The general Proof class only supports authentication using the target <see cref="SignaturePublicKey"/>,<br/>
/// but this class supports authentication using the target PublicKey, Mergers, and LpKey.<br/>
/// The authentication key is determined as follows: <br/>
/// If Signer is 0, the target PublicKey is used;<br/>
/// if it is between 1 and MergerCount, a Merger is used;<br/>
/// otherwise, LpKey is used.
/// </summary>
[TinyhandUnion(0, typeof(TestLinkageProof))]
[TinyhandObject(ReservedKeyCount = ReservedKeyCount)]
public abstract partial class LinkageProof : Proof
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public new const int ReservedKeyCount = Proof.ReservedKeyCount + 2;

    #region FieldAndProperty

    public abstract PermittedSigner PermittedSigner { get; }

    [Key(Proof.ReservedKeyCount + 0)]
    public Value Value { get; protected set; } = default!;

    /// <summary>
    /// Gets the signer index indicating which key is used for authentication.<br/>
    /// If <c>0</c>, the target <see cref="Value.Owner"/> is used.<br/>
    /// If between <c>1</c> and <c>Value.Credit.MergerCount</c>, a merger key is used.<br/>
    /// Otherwise, the <see cref="LpConstants.LpPublicKey"/> is used.
    /// </summary>
    [Key(Proof.ReservedKeyCount + 1)]
    public int Signer { get; private set; }

    [Key(Proof.ReservedKeyCount + 2)]
    public SignaturePublicKey LinkerPublicKey { get; private set; }

    #endregion

    public LinkageProof(Value value, SignaturePublicKey linkerPublicKey)
    {
        this.Value = value;
        this.LinkerPublicKey = linkerPublicKey;
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

    public override bool TryGetValue([MaybeNullWhen(false)] out Value value)
    {
        value = this.Value;
        return true;
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

        if (this.Signer == 0)
        {
            return this.PermittedSigner.HasFlag(PermittedSigner.Owner);
        }
        else if (this.Signer <= LpConstants.MaxMergers)
        {
            return this.PermittedSigner.HasFlag(PermittedSigner.Merger);
        }
        else
        {
            return this.PermittedSigner.HasFlag(PermittedSigner.LpKey);
        }
    }

    internal void PrepareSignInternal(SeedKey seedKey, long validMics)
    {
        this.PrepareSignInternal(validMics);

        var publicKey = seedKey.GetSignaturePublicKey();
        if (publicKey.Equals(this.Value.Owner))
        {// Owner
            this.Signer = 0;
        }
        else if (publicKey.Equals(LpConstants.LpPublicKey))
        {// LpKey
            this.Signer = -1;
        }
        else if (publicKey.Equals(this.Value.Credit.Mergers[0]))
        {// Merger-0
            this.Signer = 1;
        }
        else if (publicKey.Equals(this.Value.Credit.Mergers[1]))
        {// Merger-1
            this.Signer = 2;
        }
        else if (publicKey.Equals(this.Value.Credit.Mergers[2]))
        {// Merger-2
            this.Signer = 3;
        }
    }
}
