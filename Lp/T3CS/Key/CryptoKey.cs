// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Netsphere.Crypto;

namespace Lp.T3cs;

/// <summary>
/// Represents a crypto key (Raw or Encrypted SignaturePublicKey).
/// </summary>
[TinyhandObject]
public sealed partial record class CryptoKey
{
    public const int EncodedLength = sizeof(uint) + (sizeof(ulong) * 4) + EncryptedLength;
    private const int EncryptedLength = 48;
    private const uint EncryptionMask = 0xFFFFFFFE;

    #region Static

    public static bool TryCreateEncrypted(SignaturePrivateKey originalKey, EncryptionPublicKey mergerKey, uint encryption, [MaybeNullWhen(false)] out CryptoKey cryptoKey)
    {
        while ((encryption &= EncryptionMask) == 0)
        {
            encryption = RandomVault.Pseudo.NextUInt32();
        }

        Span<byte> encryptionKeySource = stackalloc byte[4 + KeyHelper.PrivateKeyLength + 4]; // Seed[4] + PrivateKey[32] + Seed[4]
        var span = encryptionKeySource;
        MemoryMarshal.Write(span, encryption);
        span = span.Slice(4);
        originalKey.UnsafeTryWriteX(span, out _);
        span = span.Slice(KeyHelper.PrivateKeyLength);
        MemoryMarshal.Write(span, encryption);

        // Span<byte> cryptoKeySource = stackalloc byte[KeyHelper.PrivateKeyLength];
        // Sha3Helper.Get256_Span(buffer, cryptoKeySource);

        var encrypted = new byte[EncryptedLength];
        var originalPublicKey = originalKey.ToPublicKey();
        var encryptionKey = EncryptionPrivateKey.Create(encryptionKeySource);
        using (var ecdh = encryptionKey.TryGetEcdh())
        using (var cache = mergerKey.TryGetEcdh())
        {
            if (ecdh is null || cache.Object is null)
            {
                cryptoKey = default;
                return false;
            }

            try
            {
                var material = ecdh.DeriveKeyMaterial(cache.Object.PublicKey);

                // Hash key material
                Sha3Helper.Get256_Span(material, material);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Key = material;

                    Span<byte> source = stackalloc byte[EncryptedLength];
                    Span<byte> iv = stackalloc byte[16];

                    span = source;
                    MemoryMarshal.Write(span, RandomVault.Pseudo.NextUInt32()); // Salt
                    span = span.Slice(4);
                    MemoryMarshal.Write(span, (uint)originalPublicKey.GetChecksum()); // Checksum
                    span = span.Slice(4);
                    originalPublicKey.TryWriteBytes(span, out _); // Original PublicKey

                    aes.TryEncryptCbc(source, iv, encrypted, out _, PaddingMode.None);
                }
            }
            catch
            {
                cryptoKey = default;
                return false;
            }
        }

        var encryptionPublicKey = encryptionKey.ToPublicKey();
        cryptoKey = new(encryption, ref encryptionPublicKey, encrypted);
        return true;
    }

    public static CryptoKey CreateRaw(SignaturePublicKey originalKey)
        => new(originalKey);

    #endregion

    #region FieldAndProperty

    [Key(0)]
    private uint encryptionAndYTilde; // 0 bit: YTilde, 1-31 bit: Encryption

    [Key(1)]
    private ulong x0; // Shared with an unencrypted public key and a public key used for encryption.

    [Key(2)]
    private ulong x1;

    [Key(3)]
    private ulong x2;

    [Key(4)]
    private ulong x3;

    [Key(5)]
    private byte[]? encrypted;

    public uint Encryption => this.encryptionAndYTilde & EncryptionMask;

    public bool IsEncrypted => this.Encryption != 0;

    private uint YTilde => this.encryptionAndYTilde & 1;

    #endregion

    public CryptoKey()
    {
    }

    private CryptoKey(uint encryption, ref EncryptionPublicKey encryptionKey, byte[] encrypted)
    {// Encrypted
        this.encryptionAndYTilde = encryption | KeyHelper.GetYTilde(encryptionKey.KeyValue);
        this.x0 = encryptionKey.X0;
        this.x1 = encryptionKey.X1;
        this.x2 = encryptionKey.X2;
        this.x3 = encryptionKey.X3;
        this.encrypted = encrypted;
    }

    private CryptoKey(SignaturePublicKey publicKey)
    {// Raw
        this.encryptionAndYTilde = KeyHelper.GetYTilde(publicKey.KeyValue);
        this.x0 = publicKey.X0;
        this.x1 = publicKey.X1;
        this.x2 = publicKey.X2;
        this.x3 = publicKey.X3;
    }

    public bool IsOriginalKey(SignaturePrivateKey originalKey, uint encryption)
    {
        if (this.IsEncrypted)
        {// Encrypted
            Span<byte> encryptionKeySource = stackalloc byte[4 + KeyHelper.PrivateKeyLength + 4]; // Seed[4] + PrivateKey[32] + Seed[4]
            var span = encryptionKeySource;
            MemoryMarshal.Write(span, encryption);
            span = span.Slice(4);
            originalKey.UnsafeTryWriteX(span, out _);
            span = span.Slice(KeyHelper.PrivateKeyLength);
            MemoryMarshal.Write(span, encryption);
            var encryptionKey = EncryptionPrivateKey.Create(encryptionKeySource);

            return originalKey.Equals(encryptionKey);
        }
        else
        {// Raw
            var originalPublicKey = originalKey.ToPublicKey();
            return originalPublicKey.X0 == this.x0 &&
                originalPublicKey.X1 == this.x1 &&
                originalPublicKey.X2 == this.x2 &&
                originalPublicKey.X3 == this.x3 &&
                this.YTilde == originalPublicKey.YTilde;
        }
    }

    public bool TryGetRawKey(out SignaturePublicKey originalKey)
    {
        if (this.IsEncrypted)
        {
            originalKey = default;
            return false;
        }

        originalKey = new SignaturePublicKey(this.x0, this.x1, this.x2, this.x3, this.YTilde);
        return true;
    }

    public bool TryGetEncryptedKey(EncryptionPrivateKey mergerKey, [MaybeNullWhen(false)] out SignaturePublicKey originalKey)
    {
        if (!this.IsEncrypted || this.encrypted is null)
        {
            originalKey = default;
            return false;
        }

        var encryptionKey = new EncryptionPublicKey(this.x0, this.x1, this.x2, this.x3, this.YTilde);
        using (var ecdh = mergerKey.TryGetEcdh())
        using (var cache = encryptionKey.TryGetEcdh())
        {
            if (ecdh is null || cache.Object is null)
            {
                originalKey = default;
                return false;
            }

            try
            {
                var material = ecdh.DeriveKeyMaterial(cache.Object.PublicKey);

                // Hash key material
                Sha3Helper.Get256_Span(material, material);

                using (var aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Key = material;

                    Span<byte> destination = stackalloc byte[EncryptedLength];
                    Span<byte> iv = stackalloc byte[16];
                    aes.TryDecryptCbc(this.encrypted, iv, destination, out _, PaddingMode.None);

                    var span = destination;
                    span = span.Slice(4); // Salt
                    var checksum = MemoryMarshal.Read<uint>(span); // Checksum
                    span = span.Slice(4);
                    var keyValue = MemoryMarshal.Read<byte>(span); // KeyValue
                    span = span.Slice(1);

                    originalKey = new SignaturePublicKey(keyValue, span);
                    if (checksum != (uint)originalKey.GetChecksum())
                    {
                        originalKey = default;
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch
            {
                originalKey = default;
                return false;
            }
        }
    }

    public override string ToString()
    {

        Span<byte> span = stackalloc byte[EncodedLength];
        this.TryWriteBytes(span, out _);
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
        MemoryMarshal.Write(b, this.encryptionAndYTilde);
        b = b.Slice(sizeof(uint));
        MemoryMarshal.Write(b, this.x0);
        b = b.Slice(sizeof(ulong));
        MemoryMarshal.Write(b, this.x1);
        b = b.Slice(sizeof(ulong));
        MemoryMarshal.Write(b, this.x2);
        b = b.Slice(sizeof(ulong));
        MemoryMarshal.Write(b, this.x3);
        b = b.Slice(sizeof(ulong));

        if (this.encrypted is null ||
            this.encrypted.Length != EncryptedLength)
        {
            written = EncodedLength - EncryptedLength;
            return true;
        }
        else
        {
            this.encrypted.CopyTo(b);
            written = EncodedLength;
            return true;
        }
    }
}
