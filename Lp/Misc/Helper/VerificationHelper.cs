// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp;

public static class VerificationHelper
{
    public static async Task<bool> SetAuthenticationToken(ClientConnection connection, Authority authority)
    {
        var context = connection.GetContext();
        var token = new AuthenticationToken(connection.Salt);
        authority.Sign(token);
        if (context.AuthenticationTokenEquals(token.PublicKey))
        {
            return true;
        }

        var result = await connection.SetAuthenticationToken(token).ConfigureAwait(false);
        return result == NetResult.Success;
    }

    public static async Task<bool> SetAuthenticationToken(ClientConnection connection, Authority authority, Credit credit)
    {
        var context = connection.GetContext();
        var token = new AuthenticationToken(connection.Salt);
        authority.SignToken(credit, token);
        if (context.AuthenticationTokenEquals(token.PublicKey))
        {
            return true;
        }

        var result = await connection.SetAuthenticationToken(token).ConfigureAwait(false);
        return result == NetResult.Success;
    }

    public static Identifier GetIdentifier<T>(this T? value, int level)
        where T : ITinyhandSerialize<T>
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        writer.Level = level;
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Signature);
            var rentMemory = writer.FlushAndGetRentMemory();
            var identifier = new Identifier(Sha3Helper.Get256_UInt64(rentMemory.Span));
            rentMemory.Return();
            return identifier;
        }
        finally
        {
            writer.Dispose();
        }
    }

    /*public static ulong GetFarmHash<T>(this T? value)
        where T : ITinyhandSerialize<T>
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Selection);
            var rentMemory = writer.FlushAndGetRentMemory();
            var hash = FarmHash.Hash64(rentMemory.Span);
            rentMemory.Return();
            return hash;
        }
        finally
        {
            writer.Dispose();
        }
    }

    public static bool VerifySignature<T>(this T value, int level, Signature signature)
        where T : ITinyhandSerialize<T>
    {
        try
        {
            var identifier2 = value.GetIdentifier(level);
            return signature.PublicKey.VerifyIdentifier(identifier2, signature.Sign);
        }
        catch
        {
            return false;
        }
    }

    public static bool VerifySign<T>(this T value, int level, SignaturePublicKey publicKey, byte[] sign)
        where T : ITinyhandSerialize<T>
    {
        try
        {
            var identifier2 = value.GetIdentifier(level);
            return publicKey.VerifyIdentifier(identifier2, sign);
        }
        catch
        {
            return false;
        }
    }*/

    public static bool VerifyIdentifierAndSignature<T>(this T value, int level, Identifier identifier, Signature signature)
        where T : ITinyhandSerialize<T>
    {
        try
        {
            var identifier2 = value.GetIdentifier(level);
            if (!identifier2.Equals(identifier))
            {
                return false;
            }

            return signature.PublicKey.VerifyIdentifier(identifier2, signature.Sign);
        }
        catch
        {
            return false;
        }
    }

    /*public static bool VerifyValueToken<T>(this T value, int level, ValueToken valueToken)
        where T : ITinyhandSerialize<T>
    {
        try
        {
            if (!valueToken.Validate())
            {
                return false;
            }

            var identifier2 = value.GetIdentifier(level);

            return valueToken.Signature.PublicKey.VerifyIdentifier(identifier2, valueToken.Signature.Sign);
        }
        catch
        {
            return false;
        }
    }*/

    public static bool SignProof(this Proof value, SignaturePrivateKey privateKey, long validMics)
    {
        if (validMics > Proof.MaxExpirationMics)
        {
            return false;
        }

        var ecdsa = privateKey.TryGetEcdsa();
        if (ecdsa == null)
        {
            return false;
        }

        var writer = TinyhandWriter.CreateFromBytePool();
        writer.Level = TinyhandWriter.DefaultSignatureLevel;
        try
        {
            if (value is ProofAndPublicKey proofAndPublicKey)
            {
                proofAndPublicKey.PrepareSignInternal(privateKey, validMics);
            }
            else
            {
                if (!value.GetPublicKey().Equals(privateKey.ToPublicKey()))
                {
                    return false;
                }

                value.PrepareSignInternal(validMics);
            }

            TinyhandSerializer.SerializeObject<Proof>(ref writer, value, TinyhandSerializerOptions.Signature);
            Span<byte> hash = stackalloc byte[Sha3_256.HashLength];
            var rentMemory = writer.FlushAndGetRentMemory();
            Sha3Helper.Get256_Span(rentMemory.Span, hash);
            rentMemory.Return();

            var sign = new byte[KeyHelper.SignatureLength];
            if (!ecdsa.TrySignHash(hash, sign.AsSpan(), out var written))
            {
                return false;
            }

            value.SetSignInternal(sign);
            return true;
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Validate object members and verify that the signature is appropriate.
    /// </summary>
    /// <param name="value">The object to be verified.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <returns><see langword="true" />: Success.</returns>
    public static bool ValidateAndVerify<T>(this T value)
        where T : ITinyhandSerialize<T>, IVerifiable
    {
        if (!value.Validate())
        {
            return false;
        }

        var writer = TinyhandWriter.CreateFromBytePool();
        writer.Level = TinyhandWriter.DefaultSignatureLevel;
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Signature);
            var rentMemory = writer.FlushAndGetRentMemory();
            var result = value.PublicKey.VerifyData(rentMemory.Span, value.Signature);
            rentMemory.Return();
            return result;
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>
    /// Validate object members and verify that the signature is appropriate.
    /// </summary>
    /// <param name="proof">The proof to be verified.</param>
    /// <returns><see langword="true" />: Success.</returns>
    public static bool ValidateAndVerify(this Proof proof)
    {
        if (!proof.Validate())
        {
            return false;
        }

        var writer = TinyhandWriter.CreateFromBytePool();
        writer.Level = TinyhandWriter.DefaultSignatureLevel;
        try
        {
            TinyhandSerializer.SerializeObject<Proof>(ref writer, proof, TinyhandSerializerOptions.Signature);
            var rentMemory = writer.FlushAndGetRentMemory();
            var result = proof.GetPublicKey().VerifyData(rentMemory.Span, proof.Signature);
            rentMemory.Return();
            return result;
        }
        finally
        {
            writer.Dispose();
        }
    }
}
