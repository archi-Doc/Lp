// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;
using System.Text;

namespace LP.T3CS;

[TinyhandObject]
public sealed partial class PrivateKey : IValidatable, IEquatable<PrivateKey>
{
    private const int MaxPrivateKeyCache = 10;

    private static ObjectCache<PrivateKey, ECDsa> PrivateKeyToEcdsa { get; } = new(MaxPrivateKeyCache);

    public static PrivateKey CreateSignatureKey()
    {
        using (var ecdsa = ECDsa.Create(PublicKey.ECCurve))
        {
            var key = ecdsa.ExportParameters(true);
            return new PrivateKey(KeyClass.T3CS_Signature, key.Q.X!, key.Q.Y!, key.D!);
        }
    }

    public static PrivateKey CreateEncryptionKey()
    {
        using (var ecdh = ECDiffieHellman.Create(PublicKey.ECCurve))
        {
            var key = ecdh.ExportParameters(true);
            return new PrivateKey(KeyClass.T3CS_Encryption, key.Q.X!, key.Q.Y!, key.D!);
        }
    }

    public static PrivateKey CreateSignatureKey(ReadOnlySpan<byte> seed)
    {
        ECParameters key = default;
        key.Curve = PublicKey.ECCurve;

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

                if (!PublicKey.CurveInstance.IsValidSeed(d))
                {
                    continue;
                }

                key.D = d;
                using (var ecdsa = ECDsa.Create(key))
                {
                    key = ecdsa.ExportParameters(true);
                    break;
                }
            }
            catch
            {
            }
        }

        return new PrivateKey(KeyClass.T3CS_Signature, key.Q.X!, key.Q.Y!, key.D!);
    }

    public static PrivateKey CreateEncryptionKey(ReadOnlySpan<byte> seed)
    {
        ECParameters key = default;
        key.Curve = PublicKey.ECCurve;

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

                if (!PublicKey.CurveInstance.IsValidSeed(d))
                {
                    continue;
                }

                key.D = d;
                using (var ecdh = ECDiffieHellman.Create(key))
                {
                    key = ecdh.ExportParameters(true);
                    break;
                }
            }
            catch
            {
            }
        }

        return new PrivateKey(KeyClass.T3CS_Encryption, key.Q.X!, key.Q.Y!, key.D!);
    }

    public static PrivateKey Create(string passphrase)
    {
        var passBytes = Encoding.UTF8.GetBytes(passphrase);
        Span<byte> span = stackalloc byte[(sizeof(ulong) + passBytes.Length) * 2]; // count, passBytes, count, passBytes // scoped
        var countSpan = span.Slice(0, sizeof(ulong));
        var countSpan2 = span.Slice(sizeof(ulong) + passBytes.Length, sizeof(ulong));
        passBytes.CopyTo(span.Slice(sizeof(ulong)));
        passBytes.CopyTo(span.Slice((sizeof(ulong) * 2) + passBytes.Length));

        return CreateSignatureKey(span);
    }

    internal PrivateKey()
    {
    }

    private PrivateKey(KeyClass keyClass, byte[] x, byte[] y, byte[] d)
    {
        this.x = x;
        this.y = y;
        this.d = d;

        var yTilde = this.CompressY();
        this.keyValue = KeyHelper.CreatePrivateKeyValue(keyClass, yTilde);
    }

    public PublicKey ToPublicKey()
        => new(this);

    public bool CreateSignature<T>(T data, out Signature signature)
        where T : ITinyhandSerialize<T>
    {// tempcode
        var ecdsa = this.TryGetEcdsa();
        if (ecdsa == null)
        {
            signature = default;
            return false;
        }

        var target = TinyhandSerializer.SerializeObject(data, TinyhandSerializerOptions.Signature);

        var sign = new byte[PublicKey.SignLength];
        if (!ecdsa.TrySignData(target, sign.AsSpan(), PublicKey.HashAlgorithmName, out var written))
        {
            signature = default;
            return false;
        }

        var mics = Mics.GetCorrected();
        signature = new Signature(this.ToPublicKey(), Signature.Type.Attest, mics, sign);
        return true;
    }

    public byte[]? SignData(ReadOnlySpan<byte> data)
    {
        var ecdsa = this.TryGetEcdsa();
        if (ecdsa == null)
        {
            return null;
        }

        var sign = new byte[PublicKey.SignLength];
        if (!ecdsa.TrySignData(data, sign.AsSpan(), PublicKey.HashAlgorithmName, out var written))
        {
            return null;
        }

        this.CacheEcdsa(ecdsa);
        return sign;
    }

    public bool SignData(ReadOnlySpan<byte> data, Span<byte> signature, out int written)
    {
        written = 0;
        if (signature.Length < PublicKey.SignLength)
        {
            return false;
        }

        var ecdsa = this.TryGetEcdsa();
        if (ecdsa == null)
        {
            return false;
        }

        if (!ecdsa.TrySignData(data, signature, PublicKey.HashAlgorithmName, out written))
        {
            return false;
        }

        PrivateKeyToEcdsa.Cache(this, ecdsa);
        return true;
    }

    public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> sign)
        => this.ToPublicKey().VerifyData(data, sign);

    #region FieldAndProperty

    [Key(0)]
    private readonly byte keyValue;

    [Key(1)]
    private readonly byte[] x = Array.Empty<byte>();

    [Key(2)]
    private readonly byte[] y = Array.Empty<byte>();

    [Key(3)]
    private readonly byte[] d = Array.Empty<byte>();

    public KeyClass KeyClass => KeyHelper.GetKeyClass(this.keyValue);

    public uint YTilde => KeyHelper.GetYTilde(this.keyValue);

    public byte[] X => this.x;

    public byte[] Y => this.y;

    #endregion

    public bool Validate()
    {
        if (this.KeyClass > KeyClass.Node_Encryption)
        {
            return false;
        }
        else if (this.x == null || this.x.Length != PublicKey.PublicKeyHalfLength)
        {
            return false;
        }
        else if (this.y == null || this.y.Length != PublicKey.PublicKeyHalfLength)
        {
            return false;
        }
        else if (this.d == null || this.d.Length != PublicKey.PrivateKeyLength)
        {
            return false;
        }

        return true;
    }

    public bool IsSameKey(PublicKey publicKey)
        => publicKey.IsSameKey(this);

    public bool Equals(PrivateKey? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.keyValue == other.keyValue &&
            this.x.AsSpan().SequenceEqual(other.x);
    }

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(this.keyValue);

        if (this.x.Length >= sizeof(ulong))
        {
            hash ^= BitConverter.ToInt32(this.x, 0);
        }

        return hash;
    }

    public string ToUnsafeString()
    {
        Span<byte> bytes = stackalloc byte[1 + NodePublicKey.PrivateKeyLength]; // scoped
        bytes[0] = this.keyValue;
        this.d.CopyTo(bytes.Slice(1));
        return $"!!!{Base64.Url.FromByteArrayToString(bytes)}!!!{this.ToPublicKey().ToString()}";
    }

    internal uint CompressY()
    {
        if (this.KeyClass == 0)
        {
            return PublicKey.CurveInstance.CompressY(this.y);
        }
        else
        {
            return 0;
        }
    }

    internal byte KeyValue => this.keyValue;

    public ECDiffieHellman? TryGetEcdh()
    {
        if (this.KeyClass != KeyClass.T3CS_Encryption)
        {
            return default;
        }

        try
        {
            ECParameters p = default;
            p.Curve = PublicKey.ECCurve;
            p.D = this.d;
            return ECDiffieHellman.Create(p);
        }
        catch
        {
        }

        return null;
    }

    internal ECDsa? TryGetEcdsa()
    {
        if (PrivateKeyToEcdsa.TryGet(this) is { } ecdsa)
        {
            return ecdsa;
        }

        if (!this.Validate())
        {
            return null;
        }

        try
        {
            ECParameters p = default;
            p.Curve = PublicKey.ECCurve;
            p.D = this.d;
            return ECDsa.Create(p);
        }
        catch
        {
        }

        return null;
    }

    private void CacheEcdsa(ECDsa ecdsa)
        => PrivateKeyToEcdsa.Cache(this, ecdsa);
}
