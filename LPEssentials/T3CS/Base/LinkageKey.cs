// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;
using Tinyhand.Tree;

namespace LP.T3CS;

/// <summary>
/// Represents a linkage key.
/// </summary>
[TinyhandObject]
public readonly partial struct LinkageKey // : IValidatable, IEquatable<LinkageKey>
{
    public LinkageKey(PublicKey publicKey)
    {// Raw
        this.Key = publicKey;
        this.checksum = (uint)publicKey.GetChecksum();
    }

    public LinkageKey(PublicKey publicKey, NodePublicKey encryptionKey)
    {// Encrypt
        this.checksum = (uint)publicKey.GetChecksum();

        encryptionKey.TryGetEcdh();
    }

    #region FieldAndProperty

    [Key(0)]
    public readonly PublicKey Key;

    [Key(1)]
    private readonly ulong encrypted0;

    [Key(2)]
    private readonly ulong encrypted1;

    [Key(3)]
    private readonly ulong encrypted2;

    [Key(4)]
    private readonly ulong encrypted3;

    [Key(5)]
    private readonly uint checksum; // FarmHash32

    #endregion

    public bool IsEncrypted => true;

    public override string ToString()
    {
        return $"[{this.ToBase64()}]";
    }

    public string ToBase64()
    {
        Span<byte> bytes = stackalloc byte[1 + (sizeof(ulong) * 4)]; // scoped
        var b = bytes;

        /*b[0] = this.keyValue;
        b = b.Slice(1);
        BitConverter.TryWriteBytes(b, this.x0);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x1);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x2);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, this.x3);
        b = b.Slice(sizeof(ulong));*/

        return $"{Base64.Url.FromByteArrayToString(bytes)}";
    }
}
