// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Arc.Crypto.EC;

namespace Netsphere.Crypto;

public static class KeyHelper
{// KeyValue 1bit: Private, 1bit: ?, 4bits: Key class, 1bit: ?, 1bit: YTilde
    public const int PublicKeyLength = 64;
    public const int PublicKeyHalfLength = PublicKeyLength / 2;
    public const int PrivateKeyLength = 32;
    public const int SignatureLength = 64;
    public const int EncodedLength = 1 + (sizeof(ulong) * 4);
    public const int ChecksumLength = 3;

    public static readonly HashAlgorithmName HashAlgorithmName;
    public static readonly ECCurveBase CurveInstance;
    public static readonly int PublicKeyLengthInBase64;

    internal static ECCurve ECCurve { get; }

    static KeyHelper()
    {
        CurveInstance = P256R1Curve.Instance;
        ECCurve = ECCurve.CreateFromFriendlyName(CurveInstance.CurveName);
        HashAlgorithmName = HashAlgorithmName.SHA256;
        PublicKeyLengthInBase64 = Base64.Url.GetEncodedLength(EncodedLength + ChecksumLength);
    }

    public static ReadOnlySpan<char> PrivateKeyBrace => "!!!";

    public static ECParameters CreateEcdhParameters()
    {
        using (var e = ECDiffieHellman.Create(KeyHelper.ECCurve))
        {
            var key = e.ExportParameters(true);
            return key;
        }
    }

    public static ECParameters CreateEcdhParameters(ReadOnlySpan<byte> seed)
    {
        ECParameters key = default;
        key.Curve = KeyHelper.ECCurve;

        byte[]? d = null;
        while (true)
        {
            try
            {
                if (d == null)
                {
                    d = Sha3Helper.Get256_ByteArray(seed);
                }
                else
                {
                    d = Sha3Helper.Get256_ByteArray(d);
                }

                if (!KeyHelper.CurveInstance.IsValidSeed(d))
                {
                    continue;
                }

                key.D = d;
                using (var e = ECDiffieHellman.Create(key))
                {
                    key = e.ExportParameters(true);
                    return key;
                }
            }
            catch
            {
            }
        }
    }

    public static ECParameters CreateEcdsaParameters()
    {
        using (var e = ECDsa.Create(KeyHelper.ECCurve))
        {
            var key = e.ExportParameters(true);
            return key;
        }
    }

    public static ECParameters CreateEcdsaParameters(ReadOnlySpan<byte> seed)
    {
        ECParameters key = default;
        key.Curve = KeyHelper.ECCurve;

        byte[]? d = null;
        while (true)
        {
            try
            {
                if (d == null)
                {
                    d = Sha3Helper.Get256_ByteArray(seed);
                }
                else
                {
                    d = Sha3Helper.Get256_ByteArray(d);
                }

                if (!KeyHelper.CurveInstance.IsValidSeed(d))
                {
                    continue;
                }

                key.D = d;
                using (var e = ECDsa.Create(key))
                {
                    key = e.ExportParameters(true);
                    return key;
                }
            }
            catch
            {
            }
        }
    }

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

    public static ECDiffieHellman? CreateEcdhFromD(byte[] d)
    {
        try
        {
            ECParameters p = default;
            p.Curve = KeyHelper.ECCurve;
            p.D = d;
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

    public static ECDsa? CreateEcdsaFromD(byte[] d)
    {
        try
        {
            ECParameters p = default;
            p.Curve = KeyHelper.ECCurve;
            p.D = d;
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

        if (chars.Length < KeyHelper.PublicKeyLengthInBase64)
        {
            keyValue = default;
            x = default;
            return false;
        }

        var bytes = Base64.Url.FromStringToByteArray(chars.Slice(0, KeyHelper.PublicKeyLengthInBase64));
        if (bytes.Length != (KeyHelper.EncodedLength + KeyHelper.ChecksumLength) ||
            !VerifyChecksum(bytes))
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

    public static void SetChecksum(Span<byte> span)
    {
        if (span.Length < 3)
        {
            throw new ArgumentOutOfRangeException();
        }

        var s = span.Slice(span.Length - 3);
        var checksum = XxHash3.Hash64(span.Slice(0, span.Length - 3));
        s[0] = (byte)(checksum & 0x0000_0000_0000_00FF);
        s[1] = (byte)((checksum & 0x0000_0000_00FF_0000) >> 16);
        s[2] = (byte)((checksum & 0x0000_00FF_0000_0000) >> 32);
    }

    public static bool VerifyChecksum(Span<byte> span)
    {
        if (span.Length < 3)
        {
            throw new ArgumentOutOfRangeException();
        }

        var s = span.Slice(span.Length - 3);
        var checksum = XxHash3.Hash64(span.Slice(0, span.Length - 3));
        if (s[0] != (byte)(checksum & 0x0000_0000_0000_00FF))
        {
            return false;
        }

        if (s[1] != (byte)((checksum & 0x0000_0000_00FF_0000) >> 16))
        {
            return false;
        }

        if (s[2] != (byte)((checksum & 0x0000_00FF_0000_0000) >> 32))
        {
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static KeyClass GetKeyClass(byte keyValue)
        => (KeyClass)((keyValue >> 2) & 15);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetYTilde(byte keyValue)
        => (uint)(keyValue & 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte ToPublicKeyValue(byte keyValue)
        => (byte)(keyValue & ~(1 << 7));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static byte CreatePrivateKeyValue(KeyClass keyClass, uint yTilde)
        => (byte)(128 | (((uint)keyClass << 2) & 15) | (yTilde & 1));

    /*[MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPrivate(byte keyValue)
        => (keyValue & 128) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsPublic(byte keyValue)
        => (keyValue & 128) == 0;*/
}
