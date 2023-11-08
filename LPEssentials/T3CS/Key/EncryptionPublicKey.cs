// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

#pragma warning disable SA1204

namespace LP.T3CS;

/// <summary>
/// Represents a public key data. Compressed to 33 bytes (memory usage 40 bytes).
/// </summary>
[TinyhandObject]
public readonly partial struct EncryptionPublicKey : IValidatable, IEquatable<EncryptionPublicKey>, IStringConvertible<EncryptionPublicKey>
{
    #region Unique

    private static ObjectCache<EncryptionPublicKey, ECDiffieHellman> Cache { get; } = new(100);

    public ObjectCache<EncryptionPublicKey, ECDiffieHellman>.Interface TryGetEcdh()
    {
        var x = new byte[32];
        this.WriteX(x);

        var e = Cache.TryGet(this) ?? KeyHelper.CreateEcdhFromX(x, this.YTilde);
        return Cache.CreateInterface(this, e);
    }

    #endregion

    #region TypeSpecific

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out EncryptionPublicKey publicKey)
    {
        if (KeyHelper.TryParsePublicKey(source, out var keyValue, out var x) &&
            KeyHelper.GetKeyClass(keyValue) == KeyClass.T3CS_Encryption)
        {
            publicKey = new(keyValue, x);
            return true;
        }

        publicKey = default;
        return false;
    }

    public static int MaxStringLength
        => KeyHelper.PublicKeyLengthInBase64;

    public int GetStringLength()
        => KeyHelper.PublicKeyLengthInBase64;

    [SkipLocalsInit]
    public bool TryFormat(Span<char> destination, out int written)
    {
        if (destination.Length < KeyHelper.PublicKeyLengthInBase64)
        {
            written = 0;
            return false;
        }

        Span<byte> span = stackalloc byte[KeyHelper.EncodedLength];
        this.TryWriteBytes(span, out _);
        return Base64.Url.FromByteArrayToSpan(span, destination, out written);
    }

    internal EncryptionPublicKey(byte keyValue, ReadOnlySpan<byte> x)
    {
        this.keyValue = KeyHelper.ToPublicKeyValue(keyValue);
        var b = x;
        this.x0 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.x1 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.x2 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.x3 = BitConverter.ToUInt64(b);
    }

    public bool IsSameKey(EncryptionPrivateKey privateKey)
    {
        if (KeyHelper.ToPublicKeyValue(privateKey.KeyValue) != this.KeyValue)
        {
            return false;
        }

        var span = privateKey.X.AsSpan();
        if (span.Length != KeyHelper.PublicKeyHalfLength)
        {
            return false;
        }

        if (this.x0 != BitConverter.ToUInt64(span))
        {
            return false;
        }

        span = span.Slice(sizeof(ulong));
        if (this.x1 != BitConverter.ToUInt64(span))
        {
            return false;
        }

        span = span.Slice(sizeof(ulong));
        if (this.x2 != BitConverter.ToUInt64(span))
        {
            return false;
        }

        span = span.Slice(sizeof(ulong));
        if (this.x3 != BitConverter.ToUInt64(span))
        {
            return false;
        }

        return true;
    }

    public bool Validate() // this.x0 != 0 && this.x1 != 0 && this.x2 != 0 && this.x3 != 0;
        => this.KeyClass == KeyClass.T3CS_Encryption;

    public bool Equals(EncryptionPublicKey other)
        => this.keyValue == other.keyValue &&
        this.x0 == other.x0 && this.x1 == other.x1 && this.x2 == other.x2 && this.x3 == other.x3;

    public override string ToString()
        => $"({this.ToBase64()})";

    #endregion

    #region Common

    [Key(0)]
    private readonly byte keyValue;

    [Key(1)]
    private readonly ulong x0;

    [Key(2)]
    private readonly ulong x1;

    [Key(3)]
    private readonly ulong x2;

    [Key(4)]
    private readonly ulong x3;

    public byte KeyValue => this.keyValue;

    public KeyClass KeyClass => KeyHelper.GetKeyClass(this.keyValue);

    public uint YTilde => KeyHelper.GetYTilde(this.keyValue);

    public bool TryWriteBytes(Span<byte> destination, out int written)
    {
        if (destination.Length < KeyHelper.EncodedLength)
        {
            written = 0;
            return false;
        }

        var b = destination;
        b[0] = this.keyValue;
        b = b.Slice(1);
        this.WriteX(b);

        written = KeyHelper.EncodedLength;
        return true;
    }

    public void WriteX(Span<byte> span)
    {
        var b = span;
        BitConverter.TryWriteBytes(b, this.x0);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x1);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x2);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x3);
    }

    public ulong GetChecksum()
    {
        Span<byte> span = stackalloc byte[KeyHelper.EncodedLength];
        this.TryWriteBytes(span, out _);
        return FarmHash.Hash64(span);
    }

    public string ToBase64()
    {
        Span<byte> span = stackalloc byte[KeyHelper.EncodedLength];
        this.TryWriteBytes(span, out _);
        return $"{Base64.Url.FromByteArrayToString(span)}";
    }

    public Identifier ToIdentifier()
        => new(this.x0, this.x1, this.x2, this.x3);

    public override int GetHashCode()
        => (int)this.x0;

    #endregion
}
