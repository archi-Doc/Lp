// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Policy;
using Arc.Collections;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject]
public partial class Linkage : IValidatable
{
    public const int SignatureLevel = TinyhandWriter.DefaultSignatureLevel + 10;

    private static readonly ObjectPool<LinkageEvidence> EvidencePool = new(() => LinkageEvidence.UnsafeConstructor());

    #region FieldAndProperty

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Ordered)]
    public long LinkedMics { get; private set; }

    [Key(1)]
    public Proof LinkageProof1 { get; private set; }

    [Key(2)]
    public Proof LinkageProof2 { get; private set; }

    [Key(3, Level = SignatureLevel + 1)]
    private byte[]? linkerSignature;

    [Key(4, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    public byte[]? MergerSignature10 { get; private set; }

    [Key(5, Level = TinyhandWriter.DefaultSignatureLevel + 2)]
    public byte[]? MergerSignature11 { get; private set; }

    [Key(6, Level = TinyhandWriter.DefaultSignatureLevel + 3)]
    public byte[]? MergerSignature12 { get; private set; }

    [Key(7, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    public byte[]? MergerSignature20 { get; private set; }

    [Key(8, Level = TinyhandWriter.DefaultSignatureLevel + 2)]
    public byte[]? MergerSignature21 { get; private set; }

    [Key(9, Level = TinyhandWriter.DefaultSignatureLevel + 3)]
    public byte[]? MergerSignature22 { get; private set; }

    #endregion

    public static bool TryCreate(LinkageEvidence evidence1, LinkageEvidence evidence2, [MaybeNullWhen(false)] out Linkage? linkage)
    {
        linkage = default;
        if (!evidence1.LinkageProof1.Equals(evidence2.LinkageProof1) ||
            !evidence1.LinkageProof2.Equals(evidence2.LinkageProof2))
        {
            return false;
        }

        if (evidence1.LinkedMics != evidence2.LinkedMics)
        {
            return false;
        }

        if (!evidence1.ValidateAndVerify() ||
            !evidence2.ValidateAndVerify())
        {
            return false;
        }

        linkage = new Linkage(evidence1.LinkageProof1, evidence1.LinkageProof2);
        linkage.LinkedMics = evidence1.LinkedMics;
        linkage.MergerSignature10 = evidence1.MergerSignature0;
        linkage.MergerSignature11 = evidence1.MergerSignature1;
        linkage.MergerSignature12 = evidence1.MergerSignature2;
        linkage.MergerSignature20 = evidence2.MergerSignature0;
        linkage.MergerSignature21 = evidence2.MergerSignature1;
        linkage.MergerSignature22 = evidence2.MergerSignature2;

        return true;
    }

    public Linkage(Proof proof1, Proof proof2)
    {
        this.LinkageProof1 = proof1;
        this.LinkageProof2 = proof2;
    }

    public bool Validate()
    {
        if (!this.LinkageProof1.TryGetLinkerPublicKey(out var linkerPublicKey) ||
            !this.LinkageProof2.TryGetLinkerPublicKey(out var linkerPublicKey2))
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

        if (!this.LinkageProof1.TryGetLinkerPublicKey(out var linkerPublicKey))
        {
            return false;
        }

        if (!this.LinkageProof1.ValidateAndVerify() ||
            !this.LinkageProof2.ValidateAndVerify())
        {
            return false;
        }

        var evidence = EvidencePool.Rent();
        try
        {
            evidence.FromLinkage(this, true);
            if (!evidence.ValidateAndVerifyExceptProof())
            {
                return false;
            }

            evidence.FromLinkage(this, false);
            if (!evidence.ValidateAndVerify())
            {
                return false;
            }
        }
        finally
        {
            EvidencePool.Return(evidence);
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
