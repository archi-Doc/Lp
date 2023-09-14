// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tinyhand.IO;
using Tinyhand.Tree;

namespace LP.T3CS;

/// <summary>
/// Represents a linkage key.
/// </summary>
[TinyhandObject]
public readonly partial struct LinkageKey // : IValidatable, IEquatable<LinkageKey>
{
    public const int EncodedLength = PublicKey.EncodedLength + sizeof(uint) + sizeof(byte) + (sizeof(ulong) * 4);

    public LinkageKey(PublicKey publicKey)
    {// Raw
        this.Key = publicKey;
        this.Checksum = (uint)publicKey.GetChecksum();
    }

    public LinkageKey(PublicKey publicKey, NodePublicKey encryptionKey)
    {// Encrypt
        this.Checksum = (uint)publicKey.GetChecksum();
        this.Key = Unsafe.As<NodePublicKey, PublicKey>(ref encryptionKey);
        this.IsEncrypted = true;
        this.keyValue = publicKey.KeyValue;

        encryptionKey.TryGetEcdh();
    }

    #region FieldAndProperty

    [Key(0)]
    public readonly PublicKey Key;

    [Key(1)]
    public readonly uint Checksum; // (uint)FarmHash.Hash64(RawKey)

    [Key(2)]
    public readonly bool IsEncrypted;

    [Key(3)]
    private readonly byte keyValue;

    [Key(4)]
    private readonly ulong encrypted0;

    [Key(5)]
    private readonly ulong encrypted1;

    [Key(6)]
    private readonly ulong encrypted2;

    [Key(7)]
    private readonly ulong encrypted3;

    #endregion

    public override string ToString()
    {
        Span<byte> span = stackalloc byte[EncodedLength]; // scoped
        this.TryWriteBytes(span, out var written);
        span = span.Slice(0, written);
        return this.IsEncrypted ? $"[{Base64.Url.FromByteArrayToString(span)}]" : $"[!{Base64.Url.FromByteArrayToString(span)}]";
    }

    public string ToBase64()
    {
        Span<byte> span = stackalloc byte[EncodedLength]; // scoped
        this.TryWriteBytes(span, out var written);
        span = span.Slice(0, written);
        return $"{Base64.Url.FromByteArrayToString(span)}";
    }

    internal bool TryWriteBytes(Span<byte> span, out int written)
    {
        if (span.Length < EncodedLength)
        {
            written = 0;
            return false;
        }

        var b = span;
        this.Key.TryWriteBytes(b, out _);
        b = b.Slice(PublicKey.EncodedLength);

        BitConverter.TryWriteBytes(b, this.Checksum);
        b = b.Slice(sizeof(uint));
        if (this.IsEncrypted)
        {
            b[0] = this.keyValue;
            b = b.Slice(1);
            BitConverter.TryWriteBytes(b, this.encrypted0);
            b = b.Slice(sizeof(ulong));
            BitConverter.TryWriteBytes(b, this.encrypted1);
            b = b.Slice(sizeof(ulong));
            BitConverter.TryWriteBytes(b, this.encrypted2);
            b = b.Slice(sizeof(ulong));
            BitConverter.TryWriteBytes(b, this.encrypted3);

            written = LinkageKey.EncodedLength;
            return true;
        }
        else
        {
            written = PublicKey.EncodedLength + sizeof(uint);
            return true;
        }
    }
}
