// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

/// <summary>
/// Immutable evidence object (authentication within merger).
/// </summary>
[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public sealed partial class Evidence : IValidatable
{
    public static bool TryCreate(Proof proof, [MaybeNullWhen(false)] out Evidence evidence)
    {
        if (!proof.TryGetCredit(out var credit))
        {
            evidence = default;
            return false;
        }

        var obj = new Evidence();
        obj.Proof = proof;

        evidence = obj;
        return true;
    }

    [Link(Primary = true, TargetMember = "ProofMics", Type = ChainType.Ordered)]
    internal Evidence()
    {
        this.Proof = default!;
    }

    #region FieldAndProperty

    [Key(0)]
    public Proof Proof { get; private set; }

    [Key(1, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    public byte[]? MergerSignature0 { get; private set; }

    [Key(2, Level = TinyhandWriter.DefaultSignatureLevel + 2)]
    public byte[]? MergerSignature1 { get; private set; }

    [Key(3, Level = TinyhandWriter.DefaultSignatureLevel + 3)]
    public byte[]? MergerSignature2 { get; private set; }

    [Key(4, Level = TinyhandWriter.DefaultSignatureLevel + 100)]
    public Proof? LinkedProof { get; private set; }

    [Key(5, Level = TinyhandWriter.DefaultSignatureLevel + 100)]
    public ulong LinkageId { get; private set; }

    public long ProofMics
        => this.Proof.VerificationMics;

    public int MergerCount
        => this.Proof.TryGetCredit(out var credit) ? credit.MergerCount : 0;

    #endregion

    public bool TrySign(SeedKey seedKey, int mergerIndex)
    {
        if (!this.Proof.TryGetCredit(out var credit))
        {
            return false;
        }

        if (credit.MergerCount <= mergerIndex ||
            !credit.Mergers[mergerIndex].Equals(seedKey.GetSignaturePublicKey()))
        {
            return false;
        }

        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        writer.Level = TinyhandWriter.DefaultSignatureLevel + mergerIndex;
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, this, TinyhandSerializerOptions.Signature);
            writer.FlushAndGetReadOnlySpan(out var span, out _);

            var sign = new byte[CryptoSign.SignatureSize];
            seedKey.Sign(span, sign);

            if (mergerIndex == 0)
            {
                this.MergerSignature0 = sign;
            }
            else if (mergerIndex == 1)
            {
                this.MergerSignature1 = sign;
            }
            else if (mergerIndex == 2)
            {
                this.MergerSignature2 = sign;
            }
            else
            {
                return false;
            }

            return true;
        }
        finally
        {
            writer.Dispose();
        }
    }

    public bool Validate()
    {
        return this.Proof.Validate();
    }

    public bool ValidateAndVerify(int mergerIndex = Credit.MaxMergers)
    {
        if (!this.Validate())
        {
            return false;
        }

        if (!this.Proof.TryGetCredit(out var credit) ||
            !this.Proof.ValidateAndVerify())
        {
            return false;
        }

        mergerIndex = Math.Max(mergerIndex, credit.MergerCount);
        if (mergerIndex >= 0 && !Verify(0, this.MergerSignature0))
        {
            return false;
        }

        if (mergerIndex >= 1 && !Verify(1, this.MergerSignature1))
        {
            return false;
        }

        if (mergerIndex >= 2 && !Verify(2, this.MergerSignature2))
        {
            return false;
        }

        return true;

        bool Verify(int mergerIndex, byte[]? signature)
        {
            if (signature is null)
            {
                return false;
            }

            var writer = TinyhandWriter.CreateFromBytePool();
            writer.Level = TinyhandWriter.DefaultSignatureLevel + mergerIndex;
            try
            {
                TinyhandSerializer.SerializeObject(ref writer, this, TinyhandSerializerOptions.Signature);
                var rentMemory = writer.FlushAndGetRentMemory();
                var result = credit.Mergers[mergerIndex].Verify(rentMemory.Span, signature);
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
    }
}
