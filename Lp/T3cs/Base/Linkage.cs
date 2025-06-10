// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

#pragma warning disable SA1401 // Fields should be private

[TinyhandObject(ReservedKeyCount = Linkage.ReservedKeyCount)]
public partial class Linkage : IValidatable
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public const int ReservedKeyCount = 10;

    public const int SignatureLevel = TinyhandWriter.DefaultSignatureLevel + 10;

    #region FieldAndProperty

    [Key(0)]
    public long LinkedMics { get; protected set; }

    [Key(1)]
    public Contract Contract1 { get; protected set; }

    [Key(2)]
    public Contract Contract2 { get; protected set; }

    [Key(3, Level = SignatureLevel + 1)]
    private byte[]? linkerSignature;

    [Key(4, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    public byte[]? MergerSignature10 { get; protected set; }

    [Key(5, Level = TinyhandWriter.DefaultSignatureLevel + 2)]
    public byte[]? MergerSignature11 { get; protected set; }

    [Key(6, Level = TinyhandWriter.DefaultSignatureLevel + 3)]
    public byte[]? MergerSignature12 { get; protected set; }

    [Key(7, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    public byte[]? MergerSignature20 { get; protected set; }

    [Key(8, Level = TinyhandWriter.DefaultSignatureLevel + 2)]
    public byte[]? MergerSignature21 { get; protected set; }

    [Key(9, Level = TinyhandWriter.DefaultSignatureLevel + 3)]
    public byte[]? MergerSignature22 { get; protected set; }

    public ContractableProof Proof1 => this.Contract1.Proof;

    public ContractableProof Proof2 => this.Contract2.Proof;

    #endregion

    public static bool TryCreate(ContractableEvidence evidence1, ContractableEvidence evidence2, [MaybeNullWhen(false)] out Linkage linkage)
        => TryCreate(() => new Linkage(), evidence1, evidence2, out linkage);

    protected static bool TryCreate<TLinkage>(Func<TLinkage> constructor, ContractableEvidence evidence1, ContractableEvidence evidence2, [MaybeNullWhen(false)] out TLinkage linkage)
        where TLinkage : Linkage
    {
        linkage = default;
        if (evidence1.IsPrimary)
        {
            if (evidence2.IsPrimary)
            {
                return false;
            }
        }
        else
        {
            if (evidence2.IsPrimary)
            {
                (evidence1, evidence2) = (evidence2, evidence1);
            }
            else
            {
                return false;
            }
        }

        if (!evidence1.Contract1.Equals(evidence2.Contract1) ||
            !evidence1.Contract2.Equals(evidence2.Contract2))
        {
            return false;
        }

        if (evidence1.LinkedMicsId != evidence2.LinkedMicsId)
        {
            return false;
        }

        if (!evidence1.ValidateAndVerify() ||
            !evidence2.ValidateAndVerify())
        {
            return false;
        }

        linkage = constructor();
        linkage.Contract1 = evidence1.Contract1;
        linkage.Contract2 = evidence1.Contract2;
        linkage.LinkedMics = evidence1.LinkedMicsId;
        linkage.MergerSignature10 = evidence1.MergerSignature0;
        linkage.MergerSignature11 = evidence1.MergerSignature1;
        linkage.MergerSignature12 = evidence1.MergerSignature2;
        linkage.MergerSignature20 = evidence2.MergerSignature0;
        linkage.MergerSignature21 = evidence2.MergerSignature1;
        linkage.MergerSignature22 = evidence2.MergerSignature2;

        return true;
    }

    protected Linkage()
    {
        this.Contract1 = default!;
        this.Contract2 = default!;
    }

    public void StripProof(ref SignaturePublicKey owner)
    {
        if (!this.Proof1.GetSignatureKey().Equals(owner))
        {
            this.Contract1 = this.Contract1.StripProof();
        }

        if (!this.Proof2.GetSignatureKey().Equals(owner))
        {
            this.Contract2 = this.Contract2.StripProof();
        }
    }

    public bool Validate() => this.Validate(out _);

    public bool ValidateAndVerify()
    {
        if (!this.Validate(out var linkerPublicKey))
        {
            return false;
        }

        var evidence = ContractableEvidence.Pool.Rent();
        try
        {
            evidence.FromLinkage(this, true);
            if (!evidence.ValidateAndVerifyExceptProof())
            {
                return false;
            }

            evidence.FromLinkage(this, false);
            if (!evidence.ValidateAndVerifyExceptProof())
            {
                return false;
            }
        }
        finally
        {
            ContractableEvidence.Pool.Return(evidence);
        }

        var writer = TinyhandWriter.CreateFromBytePool();
        writer.Level = SignatureLevel;
        try
        {
            ((ITinyhandSerializable)this).Serialize(ref writer, TinyhandSerializerOptions.Signature);
            var rentMemory = writer.FlushAndGetRentMemory();
            var result = linkerPublicKey.Verify(rentMemory.Span, this.linkerSignature);
            rentMemory.Return();
            return result;
        }
        catch
        {
            return false;
        }
        finally
        {
            writer.Dispose();
        }
    }

    internal void SetSignInternal(byte[] sign)
    {
        this.linkerSignature = sign;
    }

    private bool Validate(out SignaturePublicKey linkerPublicKey)
    {
        if (!this.Proof1.Validate() || !this.Proof2.Validate())
        {
            linkerPublicKey = default;
            return false;
        }

        var p1 = this.Proof1.TryGetLinkerPublicKey(out var publicKey);
        var p2 = this.Proof2.TryGetLinkerPublicKey(out var publicKey2);
        if (p1)
        {
            if (p2)
            {
                if (publicKey.Equals(publicKey2))
                {
                    linkerPublicKey = publicKey;
                    return true;
                }
            }
            else
            {
                linkerPublicKey = publicKey;
                return true;
            }
        }
        else
        {
            if (p2)
            {
                linkerPublicKey = publicKey2;
                return true;
            }
        }

        linkerPublicKey = default;
        return false;
    }
}
