// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Arc.Crypto.EC;

namespace LP.T3CS;

public static class KeyHelper
{// KeyValue 1bit: Private, 1bit: ?, 4bits: Key class, 1bit: ?, 1bit: YTilde
    public const int PublicKeyLength = 64;
    public const int PublicKeyHalfLength = PublicKeyLength / 2;
    public const int PrivateKeyLength = 32;
    public const int SignatureLength = 64;
    public const int EncodedLength = 1 + (sizeof(ulong) * 4);

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

    public static ECDiffieHellman? CreateEcdhFromX(byte[] x, uint yTilde)
    {
        var y = KeyHelper.CurveInstance.TryDecompressY(x, yTilde);
        if (y == null)
        {
            return null;
        }

        try
        {
            ECParameters p = default;
            p.Curve = KeyHelper.ECCurve;
            p.Q.X = x;
            p.Q.Y = y;
            return ECDiffieHellman.Create(p);
        }
        catch
        {
        }

        return null;
    }

    public static ECDsa? CreateEcdsaFromX(byte[] x, uint yTilde)
    {
        var y = KeyHelper.CurveInstance.TryDecompressY(x, yTilde);
        if (y == null)
        {
            return null;
        }

        try
        {
            ECParameters p = default;
            p.Curve = KeyHelper.ECCurve;
            p.Q.X = x;
            p.Q.Y = y;
            return ECDsa.Create(p);
        }
        catch
        {
        }

        return null;
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

    /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPrivate(byte keyValue)
        => (keyValue & 128) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPublic(byte keyValue)
        => (keyValue & 128) == 0;*/
}
