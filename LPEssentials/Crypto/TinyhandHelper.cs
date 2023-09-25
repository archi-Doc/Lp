// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LP.T3CS;
using Tinyhand.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        // var owner = ByteArrayPool.Default.Rent(BufferLength);

        var buffer = RentBuffer();
        var writer = new TinyhandWriter(buffer) { Level = level, };
        try
        {
            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Signature);
            return new Identifier(Sha3Helper.Get256_UInt64(writer.FlushAndGetReadOnlySpan()));
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
            return FarmHash.Hash64(writer.FlushAndGetReadOnlySpan());
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

    public static bool VerifySign<T>(this T value, int level, PublicKey publicKey, byte[] sign)
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

    public static bool SignProof<T>(this T value, PrivateKey privateKey, long proofMics)
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
            value.SetInternal(privateKey, proofMics);
            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Signature);
            Span<byte> hash = stackalloc byte[32];
            Sha3Helper.Get256_Span(writer.FlushAndGetReadOnlySpan(), hash);

            var sign = new byte[KeyHelper.SignLength];
            if (!ecdsa.TrySignHash(hash, sign.AsSpan(), out var written))
            {
                return false;
            }

            value.SignInternal(sign);
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
    public static bool ValidateAndVerify<T>(this T value)
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
            Span<byte> hash = stackalloc byte[32];
            Sha3Helper.Get256_Span(writer.FlushAndGetReadOnlySpan(), hash);

            return value.PublicKey.VerifyHash(hash, value.Signature);
        }
        finally
        {
            writer.Dispose();
            ReturnBuffer(buffer);
        }
    }
}
