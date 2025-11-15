// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arc.Collections;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.T3cs;

#pragma warning disable SA1310 // Field names should not contain underscore

[TinyhandObject]
public sealed partial record class CryptoKey : IEquatable<CryptoKey>, IStringConvertible<CryptoKey>
{// (!raw), (1234!raw), (:encrypted), (1234:encrypted)
    public const int EncryptedDataSize = 32 + 32 + sizeof(uint) + sizeof(uint); // PublicKey, Encrypted, EncryptionSalt, OriginalHash
    public const int SubIdMaxLength = 10;

    public static readonly int EncryptedStringLength = Base64.Url.GetEncodedLength(EncryptedDataSize);

    private const uint SubKey_HashMask = 0x3FFU; // 10 bits
    private const uint SubKey_IdMask = ~SubKey_HashMask; // 32 bits

    #region IStringConvertible

    public static int MaxStringLength => 3 + BaseHelper.UInt32MaxDecimalChars + EncryptedStringLength; // (id:encrypted)

    public int GetStringLength()
    {
        var length = 3;
        if (this.IsEncrypted)
        {
            length += EncryptedStringLength;
        }
        else
        {
            length += SeedKeyHelper.RawPublicKeyLengthInBase64;
        }

        if (this.subKey != 0)
        {
            length += BaseHelper.CountDecimalChars(this.subKey);
        }

        return length;
    }

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out CryptoKey @object, out int read, IConversionOptions? conversionOptions = null)
    {// (:encrypted), (!raw), (id:encrypted), (id!raw)
        uint subKey = 0;
        @object = null;
        if (source.Length < 3 || source.Length > MaxStringLength)
        {
            goto Failure;
        }

        if (source[0] != SeedKeyHelper.PublicKeyOpenBracket)
        {
            goto Failure;
        }

        var last = source.IndexOf(SeedKeyHelper.PublicKeyCloseBracket);
        if (last < 0)
        {
            goto Failure;
        }

        source = source.Slice(1, last - 1);
        read = last + 1;

        // :encrypted, !raw, id:encrypted, id!raw
        if (source[0] == SeedKeyHelper.PublicKeySeparator)
        {// :encrypted
            return TryParseEncrypted(subKey, source.Slice(1), out @object, conversionOptions);
        }
        else if (source[0] == SeedKeyHelper.PublicKeySeparator2)
        {// !raw
            return TryParseRaw(subKey, source.Slice(1), out @object, conversionOptions);
        }

        var encryptedIndex = source.IndexOf(SeedKeyHelper.PublicKeySeparator);
        if (encryptedIndex > 0)
        {// id:encrypted
            if (!uint.TryParse(source.Slice(0, encryptedIndex), out subKey) ||
                !ValidateSubKey(subKey))
            {
                return false;
            }

            source = source.Slice(encryptedIndex + 1);
            return TryParseEncrypted(subKey, source, out @object, conversionOptions);
        }

        var rawIndex = source.IndexOf(SeedKeyHelper.PublicKeySeparator2);
        if (rawIndex > 0)
        {// id:raw
            if (!uint.TryParse(source.Slice(0, rawIndex), out subKey) ||
                !ValidateSubKey(subKey))
            {
                return false;
            }

            source = source.Slice(rawIndex + 1);
            return TryParseRaw(subKey, source, out @object, conversionOptions);
        }

Failure:
        read = 0;
        return false;

        bool TryParseEncrypted(uint subKey, ReadOnlySpan<char> source, [MaybeNullWhen(false)] out CryptoKey? @object, IConversionOptions? conversionOptions)
        {
            Span<byte> destination = stackalloc byte[EncryptedDataSize];
            if (!Base64.Url.FromStringToSpan(source, destination, out var w) ||
                w != EncryptedDataSize)
            {
                @object = null;
                return false;
            }

            @object = new CryptoKey(subKey, destination);
            return true;
        }

        bool TryParseRaw(uint subKey, ReadOnlySpan<char> source, [MaybeNullWhen(false)] out CryptoKey? @object, IConversionOptions? conversionOptions)
        {
            if (!SignaturePublicKey.TryParse(source, out var publicKey, out _, conversionOptions))
            {
                @object = null;
                return false;
            }

            @object = new CryptoKey(ref publicKey, false);
            @object.subKey = subKey;
            return true;
        }
    }

    public bool TryFormat(Span<char> destination, out int written, IConversionOptions? conversionOptions = null)
    {
        if (destination.Length < this.GetStringLength())
        {
            written = 0;
            return false;
        }

        int w;
        var span = destination;
        span[0] = SeedKeyHelper.PublicKeyOpenBracket;
        span = span.Slice(1);

        if (this.subKey != 0)
        {// (id:encrypted), (id!raw)
            if (!this.subKey.TryFormat(span, out w))
            {
                written = 0;
                return false;
            }

            span = span.Slice(w);
        }

        if (this.IsEncrypted)
        {// (:encrypted), (id:encrypted)
            span[0] = SeedKeyHelper.PublicKeySeparator;
            span = span.Slice(1);

            Span<byte> encrypted = stackalloc byte[EncryptedDataSize];
            this.WriteEncryptedSpan(encrypted);
            Base64.Url.FromByteArrayToSpan(encrypted, span, out w);
            span = span.Slice(w);
        }
        else
        {// (!raw), (id!raw)
            span[0] = SeedKeyHelper.PublicKeySeparator2;
            span = span.Slice(1);

            var publicKey = new SignaturePublicKey(this.x0, this.x1, this.x2, this.x3);
            if (!publicKey.TryFormatWithoutBracket(span, out w, conversionOptions))
            {
                written = 0;
                return false;
            }

            span = span.Slice(w);
        }

        span[0] = SeedKeyHelper.PublicKeyCloseBracket;
        span = span.Slice(1);

        written = destination.Length - span.Length;
        return true;
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe uint GenerateSubKey()
    {
        var id = RandomVault.Default.NextUInt32() & SubKey_IdMask;
        ReadOnlySpan<byte> bytes = new ReadOnlySpan<byte>(&id, sizeof(uint));
        return id | ((uint)XxHash3Slim.Hash64(bytes) & SubKey_HashMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe bool ValidateSubKey(uint subId)
    {
        var id = subId & SubKey_IdMask;
        ReadOnlySpan<byte> bytes = new ReadOnlySpan<byte>(&id, sizeof(uint));
        var hash = (uint)XxHash3Slim.Hash64(bytes);
        return (subId & SubKey_HashMask) == (hash & SubKey_HashMask);
    }

    #region FieldAndProperty

    [Key(0)]
    private readonly ulong x0; // Shared with an unencrypted public key and a public key used for encryption.

    [Key(1)]
    private readonly ulong x1;

    [Key(2)]
    private readonly ulong x2;

    [Key(3)]
    private readonly ulong x3;

    [Key(4)]
    private byte[]? encrypted;

    [Key(5, Level = TinyhandWriter.DefaultLevel)]
    private byte[]? decrypted;

    [Key(6)]
    private uint subKey;

    [Key(7)]
    private uint encryptionSalt;

    [Key(8)]
    private uint originalHash;

    public bool IsEncrypted => this.encrypted is not null;

    public bool IsDecrypted => this.decrypted is not null;

    public uint SubKey => this.subKey;

    #endregion

    public CryptoKey(ref SignaturePublicKey publicKey, bool subId = false)
    {// Raw
        var b = publicKey.AsSpan();
        this.x0 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.x1 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.x2 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.x3 = BitConverter.ToUInt64(b);

        if (subId)
        {
            this.subKey = GenerateSubKey();
        }
    }

    public unsafe CryptoKey(SeedKey originalSeedKey, ref EncryptionPublicKey mergerPublicKey, bool subId = false)
    {// Encrypt
        if (subId)
        {
            this.subKey = GenerateSubKey();
        }

        var originalPublicKeySpan = originalSeedKey.GetSignaturePublicKey().AsSpan();
        var salt = RandomVault.Default.NextUInt32();
        this.encryptionSalt = salt;
        this.originalHash = (uint)XxHash3Slim.Hash64(originalPublicKeySpan);

        var temporalKey = SeedKey.New(originalSeedKey, new ReadOnlySpan<byte>(&salt, sizeof(uint)));
        Span<byte> material = stackalloc byte[CryptoBox.KeyMaterialSize + sizeof(uint)]; // KeyMaterial + Salt
        temporalKey.DeriveKeyMaterial(mergerPublicKey, material.Slice(0, CryptoBox.KeyMaterialSize));
        MemoryMarshal.Write(material.Slice(CryptoBox.KeyMaterialSize), salt); // Salt
        Blake3.Get256_Span(material, material.Slice(0, Blake3.Size));

        byte[] ciphertext = new byte[originalPublicKeySpan.Length];
        Aegis128L.Encrypt(ciphertext, originalPublicKeySpan, material.Slice(0, Aegis128L.NonceSize), material.Slice(Aegis128L.NonceSize, Aegis128L.KeySize), default, 0);
        material.Clear();

        this.encrypted = ciphertext;

        var b = temporalKey.GetEncryptionPublicKeySpan();
        this.x0 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.x1 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.x2 = BitConverter.ToUInt64(b);
        b = b.Slice(sizeof(ulong));
        this.x3 = BitConverter.ToUInt64(b);
    }

    private CryptoKey(uint subKey, ReadOnlySpan<byte> span)
    {
        if (span.Length != EncryptedDataSize)
        {
            throw new InvalidOperationException();
        }

        this.subKey = subKey;
        this.x0 = MemoryMarshal.Read<ulong>(span);
        span = span.Slice(sizeof(ulong));
        this.x1 = MemoryMarshal.Read<ulong>(span);
        span = span.Slice(sizeof(ulong));
        this.x2 = MemoryMarshal.Read<ulong>(span);
        span = span.Slice(sizeof(ulong));
        this.x3 = MemoryMarshal.Read<ulong>(span);
        span = span.Slice(sizeof(ulong));

        this.encrypted = span.Slice(0, 32).ToArray();
        span = span.Slice(32);
        this.encryptionSalt = MemoryMarshal.Read<uint>(span);
        span = span.Slice(sizeof(uint));
        this.originalHash = MemoryMarshal.Read<uint>(span);
        span = span.Slice(sizeof(uint));
    }

    public bool TryGetPublicKey(out SignaturePublicKey publicKey)
    {
        if (this.IsEncrypted)
        {
            if (this.decrypted is not null)
            {
                publicKey = new(this.decrypted);
                return true;
            }
            else
            {
                publicKey = default;
                return false;
            }
        }
        else
        {// Raw public key
            publicKey = new(this.x0, this.x1, this.x2, this.x3);
            return true;
        }
    }

    public bool TryDecrypt(SeedKey mergerSeedKey)
    {
        if (!this.IsEncrypted || this.IsDecrypted)
        {
            return true;
        }

        var publicKey = new EncryptionPublicKey(this.x0, this.x1, this.x2, this.x3);
        Span<byte> material = stackalloc byte[CryptoBox.KeyMaterialSize + sizeof(uint)]; // KeyMaterial + Salt
        mergerSeedKey.DeriveKeyMaterial(publicKey, material.Slice(0, CryptoBox.KeyMaterialSize));
        MemoryMarshal.Write(material.Slice(CryptoBox.KeyMaterialSize), this.encryptionSalt); // Salt
        Blake3.Get256_Span(material, material.Slice(0, Blake3.Size));

        Span<byte> plaintext = stackalloc byte[SeedKeyHelper.PublicKeySize];
        var result = Aegis128L.TryDecrypt(plaintext, this.encrypted, material.Slice(0, Aegis128L.NonceSize), material.Slice(Aegis128L.NonceSize, Aegis128L.KeySize), default, 0);
        material.Clear();

        if (!result)
        {
            return false;
        }

        if ((uint)XxHash3Slim.Hash64(plaintext) != this.originalHash)
        {// Original public key hash does not match.
            return false;
        }

        this.decrypted = plaintext.ToArray();
        return true;
    }

    public unsafe bool TryDecrypt(SeedKey originalSeedKey, ref EncryptionPublicKey mergerPublicKey)
    {
        if (!this.IsEncrypted || this.IsDecrypted)
        {
            return true;
        }

        var salt = this.encryptionSalt;
        var temporalKey = SeedKey.New(originalSeedKey, new ReadOnlySpan<byte>(&salt, sizeof(uint)));
        Span<byte> material = stackalloc byte[CryptoBox.KeyMaterialSize + sizeof(uint)]; // KeyMaterial + Salt
        temporalKey.DeriveKeyMaterial(mergerPublicKey, material.Slice(0, CryptoBox.KeyMaterialSize));
        MemoryMarshal.Write(material.Slice(CryptoBox.KeyMaterialSize), salt); // Salt
        Blake3.Get256_Span(material, material.Slice(0, Blake3.Size));

        Span<byte> plaintext = stackalloc byte[SeedKeyHelper.PublicKeySize];
        var result = Aegis128L.TryDecrypt(plaintext, this.encrypted, material.Slice(0, Aegis128L.NonceSize), material.Slice(Aegis128L.NonceSize, Aegis128L.KeySize), default, 0);
        material.Clear();

        if (!result)
        {
            return false;
        }

        if ((uint)XxHash3Slim.Hash64(plaintext) != this.originalHash)
        {// Original public key hash does not match.
            return false;
        }

        this.decrypted = plaintext.ToArray();
        return true;
    }

    public bool ValidateSubKey()
        => this.subKey == 0 ? true : ValidateSubKey(this.subKey);

    public void ClearDecrypted()
    {
        if (this.decrypted is not null)
        {
            this.decrypted.AsSpan().Clear();
            this.decrypted = null;
        }
    }

    public override int GetHashCode()
        => (int)this.x0;

    public bool Equals(CryptoKey? other)
    {
        if (other is null)
        {
            return false;
        }

        if (this.x0 != other.x0 ||
            this.x1 != other.x1 ||
            this.x2 != other.x2 ||
            this.x3 != other.x3)
        {
            return false;
        }

        if (this.encrypted is null)
        {
            if (other.encrypted is null)
            {
                return this.subKey == other.subKey;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (other.encrypted is null)
            {
                return false;
            }

            return this.subKey == other.subKey &&
                this.encryptionSalt == other.encryptionSalt &&
                this.originalHash == other.originalHash &&
                this.encrypted.AsSpan().SequenceEqual(other.encrypted.AsSpan());
        }
    }

    public override string ToString() => this.ConvertToString();

    public string ToString(IConversionOptions? conversionOptions) => this.ConvertToString(conversionOptions);

    private void WriteEncryptedSpan(Span<byte> span)
    {
        if (span.Length != EncryptedDataSize ||
            this.encrypted?.Length != 32)
        {
            throw new InvalidOperationException();
        }

        MemoryMarshal.Write(span, this.x0);
        span = span.Slice(sizeof(ulong));
        MemoryMarshal.Write(span, this.x1);
        span = span.Slice(sizeof(ulong));
        MemoryMarshal.Write(span, this.x2);
        span = span.Slice(sizeof(ulong));
        MemoryMarshal.Write(span, this.x3);
        span = span.Slice(sizeof(ulong));

        this.encrypted.AsSpan().CopyTo(span);
        span = span.Slice(32);
        MemoryMarshal.Write(span, this.encryptionSalt);
        span = span.Slice(sizeof(uint));
        MemoryMarshal.Write(span, this.originalHash);
        span = span.Slice(sizeof(uint));
    }
}

/*/// <summary>
/// Represents a crypto key (Raw or Encrypted SignaturePublicKey).
/// </summary>
[TinyhandObject]
public sealed partial record class CryptoKey : IStringConvertible<CryptoKey>, IEquatable<CryptoKey>
{
    public const int EncodedLength = 36 + EncryptedLength;
    private const int RawLength = sizeof(byte) + (sizeof(ulong) * 4); // 33
    private const int EncryptedLength = 48;
    private const int RawStringLength = (RawLength * 4 / 3) + 3;
    private const uint EncryptionMask = 0xFFFFFFFE;

    #region Static

    public static int MaxStringLength => (EncodedLength * 4 / 3) + 2;

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out CryptoKey? instance)
    {
        var span = source.Trim();
        if (span.Length < RawStringLength)
        {
            instance = default;
            return false;
        }

        if (span[0] != '[' || span[^1] != ']')
        {
            instance = default;
            return false;
        }

        Span<byte> destination = stackalloc byte[EncodedLength];
        int written;
        if (span[1] == '!')
        {// Raw
            if (span.Length != RawStringLength ||
                !Base64.Url.FromStringToSpan(span.Slice(2, span.Length - 3), destination, out written))
            {
                instance = default;
                return false;
            }
        }
        else
        {
            if (span.Length != MaxStringLength ||
                !Base64.Url.FromStringToSpan(span.Slice(1, span.Length - 2), destination, out written))
            {
                instance = default;
                return false;
            }
        }

        instance = new(destination.Slice(0, written));
        return true;
    }

    public static bool TryCreateEncrypted(SignaturePrivateKey originalKey, EncryptionPublicKey mergerKey, uint encryption, [MaybeNullWhen(false)] out CryptoKey cryptoKey)
    {
        while ((encryption &= EncryptionMask) == 0)
        {
            encryption = RandomVault.Default.NextUInt32();
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
                    MemoryMarshal.Write(span, RandomVault.Default.NextUInt32()); // Salt
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

    private byte KeyValue => KeyHelper.CreatePublicKeyValue(KeyClass.Signature, this.YTilde);

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

    private CryptoKey(ReadOnlySpan<byte> bytes)
    {// Byte array
        var span = bytes;

        if (bytes.Length == RawLength)
        {
            this.encryptionAndYTilde = KeyHelper.GetYTilde(span[0]);
            span = span.Slice(sizeof(byte));
            this.x0 = MemoryMarshal.Read<ulong>(span);
            span = span.Slice(sizeof(ulong));
            this.x1 = MemoryMarshal.Read<ulong>(span);
            span = span.Slice(sizeof(ulong));
            this.x2 = MemoryMarshal.Read<ulong>(span);
            span = span.Slice(sizeof(ulong));
            this.x3 = MemoryMarshal.Read<ulong>(span);
        }
        else if (bytes.Length == EncodedLength)
        {
            this.encryptionAndYTilde = MemoryMarshal.Read<uint>(span);
            span = span.Slice(sizeof(uint));
            this.x0 = MemoryMarshal.Read<ulong>(span);
            span = span.Slice(sizeof(ulong));
            this.x1 = MemoryMarshal.Read<ulong>(span);
            span = span.Slice(sizeof(ulong));
            this.x2 = MemoryMarshal.Read<ulong>(span);
            span = span.Slice(sizeof(ulong));
            this.x3 = MemoryMarshal.Read<ulong>(span);
            span = span.Slice(sizeof(ulong));

            this.encrypted = span.ToArray();
        }
        else
        {
            throw new InvalidDataException();
        }
    }

    public bool IsOriginalKey(SignaturePrivateKey originalKey)
    {
        if (this.IsEncrypted)
        {// Encrypted
            var encryption = this.Encryption & EncryptionMask;
            Span<byte> encryptionKeySource = stackalloc byte[4 + KeyHelper.PrivateKeyLength + 4]; // Seed[4] + PrivateKey[32] + Seed[4]
            var span = encryptionKeySource;
            MemoryMarshal.Write(span, encryption);
            span = span.Slice(4);
            originalKey.UnsafeTryWriteX(span, out _);
            span = span.Slice(KeyHelper.PrivateKeyLength);
            MemoryMarshal.Write(span, encryption);
            var encryptionKey = EncryptionPrivateKey.Create(encryptionKeySource).ToPublicKey();

            return this.x0 == encryptionKey.X0 &&
                this.x1 == encryptionKey.X1 &&
                this.x2 == encryptionKey.X2 &&
                this.x3 == encryptionKey.X3 &&
                this.YTilde == encryptionKey.YTilde;
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
        this.TryWriteBytes(span, out var w);
        span = span.Slice(0, w);
        return this.IsEncrypted ? $"[{Base64.Url.FromByteArrayToString(span)}]" : $"[!{Base64.Url.FromByteArrayToString(span)}]";
    }

    public override int GetHashCode()
    {
        if (this.encrypted is null)
        {
            return HashCode.Combine(this.encryptionAndYTilde, this.x0, this.x1, this.x2, this.x3);
        }
        else
        {
            return HashCode.Combine(this.encryptionAndYTilde, this.x0, this.x1, this.x2, this.x3, this.encrypted);
        }
    }

    public bool Equals(CryptoKey? other)
    {
        if (other is null)
        {
            return false;
        }

        if (this.encrypted is null)
        {
            if (other.encrypted is not null)
            {
                return false;
            }
        }
        else
        {
            if (other.encrypted is null)
            {
                return false;
            }

            if (this.encrypted.SequenceEqual(other.encrypted) == false)
            {
                return false;
            }
        }

        return this.encryptionAndYTilde == other.encryptionAndYTilde &&
            this.x0 == other.x0 &&
            this.x1 == other.x1 &&
            this.x2 == other.x2 &&
            this.x3 == other.x3;
    }

    public string ToBase64()
    {
        Span<byte> span = stackalloc byte[EncodedLength];
        this.TryWriteBytes(span, out var written);
        span = span.Slice(0, written);
        return $"{Base64.Url.FromByteArrayToString(span)}";
    }

    public int GetStringLength()
        => this.IsEncrypted ? MaxStringLength : RawStringLength;

    public bool TryFormat(Span<char> destination, out int written)
    {
        if (destination.Length < this.GetStringLength())
        {
            written = 0;
            return false;
        }

        Span<byte> span = stackalloc byte[EncodedLength];
        this.TryWriteBytes(span, out var w);

        var c = destination;
        c[0] = '[';
        c = c.Slice(1);
        if (!this.IsEncrypted)
        {
            c[0] = '!';
            c = c.Slice(1);
        }

        Base64.Url.FromByteArrayToSpan(span, c, out written);
        c = c.Slice(written);
        c[0] = ']';

        written += this.IsEncrypted ? 2 : 3;
        return true;
    }

    private bool TryWriteBytes(Span<byte> span, out int written)
    {
        if (span.Length < EncodedLength)
        {
            written = 0;
            return false;
        }

        var b = span;
        if (!this.IsEncrypted)
        {
            b[0] = this.KeyValue;
            b = b.Slice(sizeof(byte));
        }
        else
        {
            MemoryMarshal.Write(b, this.encryptionAndYTilde);
            b = b.Slice(sizeof(uint));
        }

        MemoryMarshal.Write(b, this.x0);
        b = b.Slice(sizeof(ulong));
        MemoryMarshal.Write(b, this.x1);
        b = b.Slice(sizeof(ulong));
        MemoryMarshal.Write(b, this.x2);
        b = b.Slice(sizeof(ulong));
        MemoryMarshal.Write(b, this.x3);
        b = b.Slice(sizeof(ulong));

        if (this.IsEncrypted)
        {
            this.encrypted.CopyTo(b);
            written = EncodedLength;
            return true;
        }
        else
        {
            written = RawLength;
            return true;
        }
    }
}*/
