// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Netsphere.Crypto;

namespace Lp.T3cs;

/// <summary>
/// Represents a linkage key.
/// </summary>
[TinyhandObject]
public readonly partial record struct LinkageKey
{
    public const int EncodedLength = KeyHelper.EncodedLength + sizeof(uint) + sizeof(byte) + (sizeof(ulong) * 4);

    public static LinkageKey CreateRaw(SignaturePublicKey publicKey)
        => new(publicKey);

    public static bool TryCreateEncrypted(SignaturePublicKey publicKey, EncryptionPublicKey encryptionKey, out LinkageKey linkageKey)
    {
        Span<byte> destination = stackalloc byte[32];

        var newKey = EncryptionPrivateKey.Create();
        using (var ecdh = newKey.TryGetEcdh())
        using (var cache2 = encryptionKey.TryGetEcdh())
        {
            if (ecdh is null || cache2.Object is null)
            {
                linkageKey = default;
                return false;
            }

            try
            {
                var material = ecdh.DeriveKeyMaterial(cache2.Object.PublicKey);

                // Hash key material
                Sha3Helper.Get256_Span(material, material);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Key = material;

                    Span<byte> source = stackalloc byte[32];
                    Span<byte> iv = stackalloc byte[16];
                    publicKey.WriteX(source);
                    aes.TryEncryptCbc(source, iv, destination, out _, PaddingMode.None);
                }
            }
            catch
            {
                linkageKey = default;
                return false;
            }
        }

        linkageKey = new(publicKey, newKey, destination);
        return true;
    }

    private LinkageKey(SignaturePublicKey publicKey)
    {// Raw
        this.Key = publicKey;
        this.Checksum = (uint)publicKey.GetChecksum();
    }

    private LinkageKey(SignaturePublicKey publicKey, EncryptionPrivateKey encryptionKey, ReadOnlySpan<byte> encrypted)
    {// Encrypted
        var encryptionPublicKey = encryptionKey.ToPublicKey();
        this.Key = Unsafe.As<EncryptionPublicKey, SignaturePublicKey>(ref encryptionPublicKey);
        this.Checksum = (uint)publicKey.GetChecksum();
        this.originalKeyValue = publicKey.KeyValue;

        var b = encrypted;
        this.encrypted0 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.encrypted1 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.encrypted2 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.encrypted3 = BitConverter.ToUInt64(b);
    }

    #region FieldAndProperty

    [Key(0)]
    public readonly SignaturePublicKey Key;

    [Key(1)]
    public readonly uint Checksum; // (uint)FarmHash.Hash64(RawKey)

    [Key(2)]
    private readonly byte originalKeyValue;

    [Key(3)]
    private readonly ulong encrypted0;

    [Key(4)]
    private readonly ulong encrypted1;

    [Key(5)]
    private readonly ulong encrypted2;

    [Key(6)]
    private readonly ulong encrypted3;

    public bool IsEncrypted
        => KeyHelper.GetKeyClass(this.Key.KeyValue) == KeyClass.Encryption;

    #endregion

    public bool TryDecrypt(ECDiffieHellman ecdh, out SignaturePublicKey decrypted)
    {
        if (!this.IsEncrypted)
        {
            decrypted = this.Key;
            return true;
        }

        Span<byte> destination = stackalloc byte[32];

        var key = this.Key;
        var encryptionKey = Unsafe.As<SignaturePublicKey, EncryptionPublicKey>(ref key);
        try
        {
            using (var cache2 = encryptionKey.TryGetEcdh())
            {
                if (ecdh is null || cache2.Object is null)
                {
                    decrypted = default;
                    return false;
                }

                var material = ecdh.DeriveKeyMaterial(cache2.Object.PublicKey);

                // Hash key material
                Sha3Helper.Get256_Span(material, material);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Key = material;

                    Span<byte> source = stackalloc byte[32];
                    Span<byte> iv = stackalloc byte[16];

                    var c = source;
                    BitConverter.TryWriteBytes(c, this.encrypted0);
                    c = c.Slice(sizeof(ulong));
                    BitConverter.TryWriteBytes(c, this.encrypted1);
                    c = c.Slice(sizeof(ulong));
                    BitConverter.TryWriteBytes(c, this.encrypted2);
                    c = c.Slice(sizeof(ulong));
                    BitConverter.TryWriteBytes(c, this.encrypted3);

                    aes.TryDecryptCbc(source, iv, destination, out _, PaddingMode.None);
                }
            }
        }
        catch
        {
            decrypted = default;
            return false;
        }

        decrypted = new(this.originalKeyValue, destination);
        if ((uint)decrypted.GetChecksum() != this.Checksum)
        {
            decrypted = default;
            return false;
        }

        return true;
    }

    public override string ToString()
    {
        Span<byte> span = stackalloc byte[EncodedLength];
        this.TryWriteBytes(span, out var written);
        span = span.Slice(0, written);
        return this.IsEncrypted ? $"[{Base64.Url.FromByteArrayToString(span)}]" : $"[!{Base64.Url.FromByteArrayToString(span)}]";
    }

    public string ToBase64()
    {
        Span<byte> span = stackalloc byte[EncodedLength];
        this.TryWriteBytes(span, out var written);
        span = span.Slice(0, written);
        return $"{Base64.Url.FromByteArrayToString(span)}";
    }

    private bool TryWriteBytes(Span<byte> span, out int written)
    {
        if (span.Length < EncodedLength)
        {
            written = 0;
            return false;
        }

        var b = span;
        this.Key.TryWriteBytes(b, out _);
        b = b.Slice(KeyHelper.EncodedLength);

        BitConverter.TryWriteBytes(b, this.Checksum);
        b = b.Slice(sizeof(uint));
        if (this.IsEncrypted)
        {
            b[0] = this.originalKeyValue;
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
            written = KeyHelper.EncodedLength + sizeof(uint);
            return true;
        }
    }
}
