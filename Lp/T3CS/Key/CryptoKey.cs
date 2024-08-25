// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Netsphere.Crypto;

namespace Lp.T3cs;

/// <summary>
/// Represents a crypto key.
/// 
/// </summary>
[TinyhandObject]
public partial record class CryptoKey
{
    #region FieldAndProperty

    #endregion

    public static bool TryCreate(SignaturePrivateKey privateKey, EncryptionPublicKey mergerKey, uint seed, [MaybeNullWhen(false)] out CryptoKey cryptoKey)
    {
        while (seed == 0)
        {
            seed = RandomVault.Pseudo.NextUInt32();
        }

        Span<byte> buffer = stackalloc byte[4 + KeyHelper.PrivateKeyLength + 4]; // Seed[4] + PrivateKey[32] + Seedn[4]
        Span<byte> destination = stackalloc byte[32];
        var span = buffer;
        MemoryMarshal.Write(span, seed);
        span = span.Slice(4);
        privateKey.UnsafeTryWriteX(span, out _);
        span = span.Slice(KeyHelper.PrivateKeyLength);
        MemoryMarshal.Write(span, seed);

        // Span<byte> cryptoKeySource = stackalloc byte[KeyHelper.PrivateKeyLength];
        // Sha3Helper.Get256_Span(buffer, cryptoKeySource);

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
                    privateKey.ToPublicKey().WriteX(source);
                    aes.TryEncryptCbc(source, iv, destination, out _, PaddingMode.None);
                }
            }
            catch
            {
                cryptoKey = default;
                return false;
            }
        }

        cryptoKey = new(publicKey, newKey, destination);
        return true;
    }

    public CryptoKey()
    {
    }
}
