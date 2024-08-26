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
    private const uint EncryptionMask = 0xFFFFFFFE;

    #region Static

    public static bool TryCreate(SignaturePrivateKey originalKey, EncryptionPublicKey mergerKey, uint encryption, [MaybeNullWhen(false)] out CryptoKey cryptoKey)
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

        var encrypted = new byte[48];
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

                    Span<byte> source = stackalloc byte[48];
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

    public static CryptoKey Create(SignaturePublicKey originalKey)
        => new(originalKey);

    #endregion

    #region FieldAndProperty

    [Key(0)]
    private uint encryptionAndYTilde; // 0 bit: YTilde, 1-31 bit: Encryption

    [Key(1)]
    private ulong x0;

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

    private bool YTilde => (this.encryptionAndYTilde & 1) == 1;

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

    public bool TryGetRawKey(out SignaturePublicKey signaturePublicKey)
    {
        if (this.IsEncrypted)
        {
            signaturePublicKey = default;
            return false;
        }

        signaturePublicKey = new SignaturePublicKey(0, 0, 0, 0, this.YTilde);
        return true;
    }
}
