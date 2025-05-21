// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Tinyhand.IO;

namespace Lp.T3cs;

#pragma warning disable SA1401 // Fields should be private

[TinyhandObject(ReservedKeyCount = Linkage.ReservedKeyCount)]
// [ValueLinkObject]
public partial class Linkage : IValidatable
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public const int ReservedKeyCount = 10;

    public const int SignatureLevel = TinyhandWriter.DefaultSignatureLevel + 10;

    #region FieldAndProperty

    [Key(0)]
    // [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    public long LinkedMics { get; protected set; }

    [Key(1)]
    public Proof BaseProof1 { get; protected set; }

    [Key(2)]
    public Proof BaseProof2 { get; protected set; }

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

    #endregion

    public static bool TryCreate(LinkableEvidence evidence1, LinkableEvidence evidence2, [MaybeNullWhen(false)] out Linkage linkage)
        => TryCreate(() => new Linkage(), evidence1, evidence2, out linkage);

    protected static bool TryCreate<TLinkage>(Func<TLinkage> constructor, LinkableEvidence evidence1, LinkableEvidence evidence2, [MaybeNullWhen(false)] out TLinkage linkage)
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

        if (!evidence1.BaseProof1.Equals(evidence2.BaseProof1) ||
            !evidence1.BaseProof2.Equals(evidence2.BaseProof2))
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
        linkage.BaseProof1 = evidence1.BaseProof1;
        linkage.BaseProof2 = evidence1.BaseProof2;
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
        this.BaseProof1 = default!;
        this.BaseProof2 = default!;
    }

    public bool Validate()
    {
        if (!this.BaseProof1.TryGetLinkerPublicKey(out var linkerPublicKey) ||
            !this.BaseProof2.TryGetLinkerPublicKey(out var linkerPublicKey2))
        {
            return false;
        }

        if (!linkerPublicKey.Equals(linkerPublicKey2))
        {
            return false;
        }

        return true;
    }

    public bool ValidateAndVerify()
    {
        if (!this.Validate())
        {
            return false;
        }

        if (!this.BaseProof1.TryGetLinkerPublicKey(out var linkerPublicKey))
        {
            return false;
        }

        if (!this.BaseProof1.ValidateAndVerify() ||
            !this.BaseProof2.ValidateAndVerify())
        {
            return false;
        }

        var evidence = LinkableEvidence.Pool.Rent();
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
            LinkableEvidence.Pool.Return(evidence);
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
}
