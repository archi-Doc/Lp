// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Arc.Crypto.EC;

namespace LP.T3CS;

public static class KeyHelper
{// KeyValue 1bit: Private, 1bit: ?, 4bits: Key class, 1bit: ?, 1bit: YTilde
    public const int PublicKeyLength = 64;
    public const int PublicKeyHalfLength = PublicKeyLength / 2;
    public const int PrivateKeyLength = 32;
    public const int SignLength = 64;
    public const int EncodedLength = 1 + (sizeof(ulong) * 4);
    public const KeyClass UpperKeyClass = KeyClass.Node_Encryption;

    public static readonly HashAlgorithmName HashAlgorithmName;
    public static readonly ECCurveBase CurveInstance;

    internal static ECCurve ECCurve { get; }

    static KeyHelper()
    {
        CurveInstance = P256R1Curve.Instance;
        ECCurve = ECCurve.CreateFromFriendlyName(CurveInstance.CurveName);
        HashAlgorithmName = HashAlgorithmName.SHA256;
    }

    public static ReadOnlySpan<char> PrivateKeyBrace => "!!!";

    public static unsafe ulong GetChecksum(this IPublicKey publicKey)
    {
        Span<byte> span = stackalloc byte[KeyHelper.EncodedLength];
        publicKey.TryWriteBytes(span, out _);
        return FarmHash.Hash64(span);
    }

    public static bool TryWriteBytes(this IPublicKey publicKey, Span<byte> span, out int written)
    {
        if (span.Length < KeyHelper.EncodedLength)
        {
            written = 0;
            return false;
        }

        var b = span;
        b[0] = publicKey.KeyValue;
        b = b.Slice(1);
        BitConverter.TryWriteBytes(b, publicKey.X0);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, publicKey.X1);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, publicKey.X2);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, publicKey.X3);
        b = b.Slice(sizeof(ulong));

        written = KeyHelper.EncodedLength;
        return true;
    }

    public static bool TryParsePublicKey(ReadOnlySpan<char> chars, out byte keyValue, out ReadOnlySpan<byte> x)
    {
        if (chars.Length >= 2 && chars[0] == '(' && chars[chars.Length - 1] == ')')
        {
            chars = chars.Slice(1, chars.Length - 2);
        }

        var bytes = Base64.Url.FromStringToByteArray(chars);
        if (bytes.Length != KeyHelper.EncodedLength)
        {
            keyValue = default;
            x = default;
            return false;
        }

        var b = bytes.AsSpan();
        keyValue = b[0];
        x = b.Slice(1);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte ToPublicKeyValue(byte keyValue)
        => (byte)(keyValue & ~(1 << 7));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte CreatePrivateKeyValue(KeyClass keyClass, uint yTilde)
        => (byte)(128 | (((uint)keyClass << 2) & 15) | (yTilde & 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static KeyClass GetKeyClass(byte keyValue)
        => (KeyClass)((keyValue >> 2) & 15);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint GetYTilde(byte keyValue)
        => (uint)(keyValue & 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPrivate(byte keyValue)
        => (keyValue & 128) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPublic(byte keyValue)
        => (keyValue & 128) == 0;
}
