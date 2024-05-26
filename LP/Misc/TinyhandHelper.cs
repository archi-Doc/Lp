// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using LP.T3CS;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace LP;

public static class TinyhandHelper
{
    private const int BufferLength = 64 * 1024;
    private const int BufferMax = 16;

    private static ArrayPool<byte> arrayPool { get; } = ArrayPool<byte>.Create(BufferLength, BufferMax);

    public static byte[] RentBuffer() => arrayPool.Rent(BufferLength);

    public static void ReturnBuffer(byte[] buffer) => arrayPool.Return(buffer);

    public static Identifier GetIdentifier<T>(this T? value, int level)
        where T : ITinyhandSerialize<T>
    {
        // var rentMemory = ByteArrayPool.Default.Rent(BufferLength);

        var buffer = RentBuffer();
        var writer = new TinyhandWriter(buffer) { Level = level, };
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Signature);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            return new Identifier(Sha3Helper.Get256_UInt64(span));
        }
        finally
        {
            writer.Dispose();
            ReturnBuffer(buffer);
        }
    }

    public static ulong GetFarmHash<T>(this T? value)
        where T : ITinyhandSerialize<T>
    {
        var buffer = RentBuffer();
        var writer = new TinyhandWriter(buffer);
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Selection);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            return FarmHash.Hash64(span);
        }
        finally
        {
            writer.Dispose();
            ReturnBuffer(buffer);
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
    }

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

    public static bool VerifyValueToken<T>(this T value, int level, ValueToken valueToken)
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
    }

    public static bool SignProof<T>(this T value, SignaturePrivateKey privateKey, long proofMics)
        where T : Proof, ITinyhandSerialize<T>
    {
        var ecdsa = privateKey.TryGetEcdsa();
        if (ecdsa == null)
        {
            return false;
        }

        var buffer = RentBuffer();
        var writer = new TinyhandWriter(buffer) { Level = 0, };
        try
        {
            value.SetInformationInternal(privateKey, proofMics);
            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Signature);
            Span<byte> hash = stackalloc byte[32];
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            Sha3Helper.Get256_Span(span, hash);

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
            ReturnBuffer(buffer);
        }
    }

    /// <summary>
    /// Validate object members and verify that the signature is appropriate.
    /// </summary>
    /// <param name="value">The object to be verified.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <returns><see langword="true" />: Success.</returns>
    public static bool ValidateAndVerify<T>(T value)
        where T : ITinyhandSerialize<T>, IVerifiable
    {
        if (!value.Validate())
        {
            return false;
        }

        var buffer = RentBuffer();
        var writer = new TinyhandWriter(buffer) { Level = 0, };
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Signature);
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            return value.PublicKey.VerifyData(span, value.Signature);
        }
        finally
        {
            writer.Dispose();
            ReturnBuffer(buffer);
        }
    }
}
