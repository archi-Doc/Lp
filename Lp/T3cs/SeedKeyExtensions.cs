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
            if (proof is ProofAndPublicKey proofAndPublicKey)
            {
                proofAndPublicKey.PrepareSignInternal(seedKey, validMics);
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
}
