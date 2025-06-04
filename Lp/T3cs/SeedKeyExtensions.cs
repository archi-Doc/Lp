// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

public static class SeedKeyExtensions
{
    public static bool TrySign(this SeedKey seedKey, Proof proof, long validMics)
    {
        if (validMics > proof.MaxValidMics)
        {
            return false;
        }

        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        writer.Level = TinyhandWriter.DefaultSignatureLevel;
        try
        {
            var publicKey = seedKey.GetSignaturePublicKey();
            if (!proof.PrepareForSigning(ref publicKey, validMics))
            {
                return false;
            }

            TinyhandSerializer.SerializeObject<Proof>(ref writer, proof, TinyhandSerializerOptions.Signature);
            Span<byte> hash = stackalloc byte[Blake3.Size];
            writer.FlushAndGetReadOnlySpan(out var span, out _);

            var signature = new byte[CryptoSign.SignatureSize];
            seedKey.Sign(span, signature);
            return proof.SetSignature(new(0, signature));
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static bool TrySign(this SeedKey seedKey, Evidence evidence, int mergerIndex)
    {
        if (!evidence.BaseProof.TryGetCredit(out var credit))
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
            ((ITinyhandSerializable)evidence).Serialize(ref writer, TinyhandSerializerOptions.Signature);
            writer.FlushAndGetReadOnlySpan(out var span, out _);

            var sign = new byte[CryptoSign.SignatureSize];
            seedKey.Sign(span, sign);
            return evidence.SetSignInternal(mergerIndex, sign);
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static bool TrySign(this SeedKey seedKey, Evidence evidence)
    {
        if (!evidence.BaseProof.TryGetCredit(out var credit))
        {
            return false;
        }

        var publicKey = seedKey.GetSignaturePublicKey();
        var mergerIndex = credit.GetMergerIndex(ref publicKey);
        if (mergerIndex < 0)
        {
            return false;
        }

        if (evidence.GetSignature(mergerIndex) is not null)
        {
            return true;
        }

        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        writer.Level = TinyhandWriter.DefaultSignatureLevel + mergerIndex;
        try
        {
            ((ITinyhandSerializable)evidence).Serialize(ref writer, TinyhandSerializerOptions.Signature);
            writer.FlushAndGetReadOnlySpan(out var span, out _);

            var sign = new byte[CryptoSign.SignatureSize];
            seedKey.Sign(span, sign);
            return evidence.SetSignInternal(mergerIndex, sign);
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static bool TrySign(this SeedKey seedKey, ContractableEvidence evidence)
    {
        if (!evidence.BaseProof.TryGetCredit(out var credit))
        {
            return false;
        }

        var publicKey = seedKey.GetSignaturePublicKey();
        var mergerIndex = credit.GetMergerIndex(ref publicKey);
        if (mergerIndex < 0)
        {
            return false;
        }

        if (evidence.GetSignature(mergerIndex) is not null)
        {
            return true;
        }

        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        writer.Level = TinyhandWriter.DefaultSignatureLevel + mergerIndex;
        try
        {
            ((ITinyhandSerializable)evidence).Serialize(ref writer, TinyhandSerializerOptions.Signature);
            writer.FlushAndGetReadOnlySpan(out var span, out _);

            var sign = new byte[CryptoSign.SignatureSize];
            seedKey.Sign(span, sign);
            return evidence.SetSignInternal(mergerIndex, sign);
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static bool TrySign(this SeedKey seedKey, Linkage linkage, long validMics)
    {
        if (!linkage.Proof1.TryGetLinkerPublicKey(out var linkerPublicKey))
        {
            return false;
        }

        if (!seedKey.GetSignaturePublicKey().Equals(linkerPublicKey))
        {
            return false;
        }

        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        writer.Level = Linkage.SignatureLevel - 1;
        try
        {
            TinyhandSerializer.SerializeObject<Linkage>(ref writer, linkage, TinyhandSerializerOptions.Signature);
            Span<byte> hash = stackalloc byte[Blake3.Size];
            writer.FlushAndGetReadOnlySpan(out var span, out _);

            var signature = new byte[CryptoSign.SignatureSize];
            seedKey.Sign(span, signature);
            linkage.SetSignInternal(signature);
            return true;
        }
        finally
        {
            writer.Dispose();
        }
    }
}
