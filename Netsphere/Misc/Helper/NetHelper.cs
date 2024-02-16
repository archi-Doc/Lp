﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Netsphere.Crypto;
using Netsphere.Packet;
using Tinyhand.IO;

#pragma warning disable SA1202

namespace Netsphere;

public static class NetHelper
{
    internal const int RamaGenes = 3;
    internal const char Quote = '\"';
    internal const string TripleQuotes = "\"\"\"";
    private const int BufferLength = 64 * 1024;
    private const int BufferMax = 16;

    private static ArrayPool<byte> arrayPool { get; } = ArrayPool<byte>.Create(BufferLength, BufferMax);

    private static byte[] RentBuffer()
        => arrayPool.Rent(BufferLength);

    private static void ReturnBuffer(byte[] buffer)
        => arrayPool.Return(buffer);

    public static bool TrySerialize<T>(T value, out ByteArrayPool.MemoryOwner owner)
    {
        var buffer = RentBuffer();
        try
        {
            var writer = new TinyhandWriter(buffer);
            TinyhandSerializer.Serialize(ref writer, value, TinyhandSerializerOptions.Standard);

            writer.FlushAndGetArray(out var array, out var arrayLength, out var isInitialBuffer);
            if (isInitialBuffer)
            {
                owner = new ByteArrayPool.MemoryOwner(buffer, 0, arrayLength);
                return true;
            }
            else
            {
                ReturnBuffer(buffer);
                owner = new ByteArrayPool.MemoryOwner(array);
                return true;
            }
        }
        catch
        {
            ReturnBuffer(buffer);
            owner = default;
            return false;
        }
    }

    public static bool TryDeserialize<T>(ByteArrayPool.MemoryOwner owner, [MaybeNullWhen(false)] out T value)
        => TinyhandSerializer.TryDeserialize<T>(owner.Memory.Span, out value, TinyhandSerializerOptions.Standard);

    public static bool TryDeserialize<T>(ByteArrayPool.ReadOnlyMemoryOwner owner, [MaybeNullWhen(false)] out T value)
        => TinyhandSerializer.TryDeserialize<T>(owner.Memory.Span, out value, TinyhandSerializerOptions.Standard);

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

    public static ulong GetDataId<TSend, TReceive>()
        => (ulong)Tinyhand.TinyhandHelper.GetFullNameId<TSend>() | ((ulong)Tinyhand.TinyhandHelper.GetFullNameId<TReceive>() << 32);

    public static string ToBase64<T>(this T value)
        where T : ITinyhandSerialize<T>
    {
        return Base64.Url.FromByteArrayToString(TinyhandSerializer.SerializeObject(value));
    }

    public static string To4Hex(this ulong gene) => $"{(ushort)gene:x4}";

    public static string To4Hex(this uint id) => $"{(ushort)id:x4}";

    public static string TrimQuotes(this string text)
    {
        if (text.Length >= 6 && text.StartsWith(TripleQuotes) && text.EndsWith(TripleQuotes))
        {
            return text.Substring(3, text.Length - 6);
        }
        else if (text.Length >= 2 && text.StartsWith(Quote) && text.EndsWith(Quote))
        {
            return text.Substring(1, text.Length - 2);
        }

        return text;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (int NumberOfGenes, uint FirstGeneSize, uint LastGeneSize) CalculateGene(long size)
    {// FirstGeneSize, GeneFrame.MaxBlockLength..., LastGeneSize
        if (size <= FirstGeneFrame.MaxGeneLength)
        {
            return (1, (uint)size, 0);
        }

        size -= FirstGeneFrame.MaxGeneLength;
        var numberOfGenes = (int)(size / FollowingGeneFrame.MaxGeneLength);
        var lastGeneSize = (uint)(size - (numberOfGenes * FollowingGeneFrame.MaxGeneLength));
        return (lastGeneSize > 0 ? numberOfGenes + 2 : numberOfGenes + 1, FirstGeneFrame.MaxGeneLength, lastGeneSize);
    }
}
