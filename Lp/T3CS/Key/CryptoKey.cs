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
    #region Static

    public static bool TryCreate(SignaturePrivateKey originalKey, EncryptionPublicKey mergerKey, uint encryption, [MaybeNullWhen(false)] out CryptoKey cryptoKey)
    {
        while (encryption == 0)
        {
            encryption = RandomVault.Pseudo.NextUInt32();
        }

        Span<byte> buffer = stackalloc byte[4 + KeyHelper.PrivateKeyLength + 4]; // Seed[4] + PrivateKey[32] + Seedn[4]
        var span = buffer;
        MemoryMarshal.Write(span, encryption);
        span = span.Slice(4);
        originalKey.UnsafeTryWriteX(span, out _);
        span = span.Slice(KeyHelper.PrivateKeyLength);
        MemoryMarshal.Write(span, encryption);

        // Span<byte> cryptoKeySource = stackalloc byte[KeyHelper.PrivateKeyLength];
        // Sha3Helper.Get256_Span(buffer, cryptoKeySource);

        Span<byte> destination = stackalloc byte[32];
        var originalPublicKey = originalKey.ToPublicKey();
        var encryptionKey = EncryptionPrivateKey.Create(buffer);
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

                    Span<byte> source = stackalloc byte[32];
                    Span<byte> iv = stackalloc byte[16];
                    originalPublicKey.WriteX(source);
                    aes.TryEncryptCbc(source, iv, destination, out _, PaddingMode.None);
                }
            }
            catch
            {
                cryptoKey = default;
                return false;
            }
        }

        cryptoKey = new(encryption, (uint)originalPublicKey.GetChecksum(), originalPublicKey.KeyValue, encryptionKey, destination);
        return true;
    }

    #endregion

    #region FieldAndProperty

    [Key(0)]
    private readonly uint encryption; // 0: Raw, 0<: Encrypted

    [Key(1)]
    private readonly uint checksum; // (uint)originalPublicKey.GetChecksum()

    [Key(2)]
    private readonly byte originalKeyValue;

    #endregion

    public CryptoKey()
    {
    }

    private CryptoKey(uint encryption, uint checksum, byte originalKeyValue, EncryptionPrivateKey encryptionKey, Span<byte> encrypted)
    {
        this.checksum = checksum;
        this.originalKeyValue = originalKeyValue;
        this.encryption = encryption;
    }
}
