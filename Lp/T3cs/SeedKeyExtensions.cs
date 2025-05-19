// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Netsphere;

public static class SeedKeyExtensions
{
    public static bool SignWithSalt<T>(this SeedKey seedKey, T value, ulong salt)
        where T : ITinyhandSerializable<T>, ISignAndVerify
    {
        var writer = TinyhandWriter.CreateFromThreadStaticBuffer();
        writer.Level = TinyhandWriter.DefaultSignatureLevel;
        try
        {
            value.PublicKey = seedKey.GetSignaturePublicKey();
            value.SignedMics = Mics.FastCorrected;
            value.Salt = salt;
            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Signature);
            Span<byte> hash = stackalloc byte[32];
            writer.FlushAndGetReadOnlySpan(out var span, out _);

            var sign = new byte[CryptoSign.SignatureSize];
            seedKey.Sign(span, sign);
            value.Signature = sign;
            return true;
        }
        finally
        {
            writer.Dispose();
        }
    }

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
            if (proof is ProofWithPublicKey proofAndPublicKey)
            {
                proofAndPublicKey.PrepareSignInternal(seedKey, validMics);
            }
            else if (proof is ProofWithSigner proofWithSigner)
            {
                proofWithSigner.PrepareSignInternal(seedKey, validMics);
            }
            else
            {
                if (!proof.GetSignatureKey().Equals(seedKey.GetSignaturePublicKey()))
                {
                    return false;
                }

                proof.PrepareSignInternal(validMics);
            }

            TinyhandSerializer.SerializeObject<Proof>(ref writer, proof, TinyhandSerializerOptions.Signature);
            Span<byte> hash = stackalloc byte[Blake3.Size];
            writer.FlushAndGetReadOnlySpan(out var span, out _);

            var signature = new byte[CryptoSign.SignatureSize];
            seedKey.Sign(span, signature);
            proof.SetSignInternal(signature);
            return true;
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

    public static bool TrySign(this SeedKey seedKey, Linkage linkage, long validMics)
    {
        if (!linkage.LinkageProof1.TryGetLinkerPublicKey(out var linkerPublicKey))
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
