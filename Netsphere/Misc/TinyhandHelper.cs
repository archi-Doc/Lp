// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Netsphere;

public static class TinyhandHelper
{
    private const int BufferLength = 64 * 1024;
    private const int BufferMax = 16;

    private static ArrayPool<byte> arrayPool { get; } = ArrayPool<byte>.Create(BufferLength, BufferMax);

    public static byte[] RentBuffer() => arrayPool.Rent(BufferLength);

    public static void ReturnBuffer(byte[] buffer) => arrayPool.Return(buffer);

    public static string ToBase64<T>(this T value)
        where T : ITinyhandSerialize<T>, ISignAndVerify
    {
        return Base64.Url.FromByteArrayToString(TinyhandSerializer.SerializeObject(value));
    }

    public static bool Sign<T>(this T value, SignaturePrivateKey privateKey)
        where T : ITinyhandSerialize<T>, ISignAndVerify
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
            value.PublicKey = privateKey.ToPublicKey();
            value.SignedMics = Mics.GetCorrected(); // signedMics;
            TinyhandSerializer.SerializeObject(ref writer, value, TinyhandSerializerOptions.Signature);
            Span<byte> hash = stackalloc byte[32];
            writer.FlushAndGetReadOnlySpan(out var span, out _);
            Sha3Helper.Get256_Span(span, hash);

            var sign = new byte[KeyHelper.SignatureLength];
            if (!ecdsa.TrySignHash(hash, sign.AsSpan(), out var written))
            {
                return false;
            }

            value.Signature = sign; // value.SetSignInternal(sign);
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
        where T : ITinyhandSerialize<T>, ISignAndVerify
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
