// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Arc.Crypto;

#pragma warning disable SA1204

namespace Netsphere.Crypto;

/// <summary>
/// Represents a private key data.<br/>
/// Signature: ECDsa, secp256r1.
/// </summary>
[TinyhandObject]
public sealed partial class SignaturePrivateKey : PrivateKeyBase, IEquatable<SignaturePrivateKey>, IStringConvertible<SignaturePrivateKey>
{
    #region Unique

    private ECDsa? ecdsa;

    public static int MaxStringLength => UnsafeStringLength;

    public int GetStringLength() => UnsafeStringLength;

    public bool TryFormat(Span<char> destination, out int written)
        => this.UnsafeTryFormat(destination, out written);

    public static bool TryParse(ReadOnlySpan<char> base64url, [MaybeNullWhen(false)] out SignaturePrivateKey privateKey)
    {
        if (TryParseKey(KeyClass.T3CS_Signature, base64url, out var key))
        {
            privateKey = new SignaturePrivateKey(key.Q.X!, key.Q.Y!, key.D!);
            return true;
        }
        else
        {
            privateKey = default;
            return false;
        }
    }

    public static SignaturePrivateKey Create()
    {
        var p = KeyHelper.CreateEcdsaParameters();
        return new SignaturePrivateKey(p.Q.X!, p.Q.Y!, p.D!);
    }

    public static SignaturePrivateKey Create(ReadOnlySpan<byte> seed)
    {
        var p = KeyHelper.CreateEcdsaParameters(seed);
        return new SignaturePrivateKey(p.Q.X!, p.Q.Y!, p.D!);
    }

    public ECDiffieHellman? TryGetEcdh()
    {
        return KeyHelper.CreateEcdhFromD(this.d);
    }

    public ECDsa? TryGetEcdsa()
    {
        return this.ecdsa ??= KeyHelper.CreateEcdsaFromD(this.d);
    }

    /*public bool CreateSignature<T>(T data, out Signature signature)
        where T : ITinyhandSerialize<T>
    {
        var ecdsa = this.TryGetEcdsa();
        if (ecdsa == null)
        {
            signature = default;
            return false;
        }

        var target = TinyhandSerializer.SerializeObject(data, TinyhandSerializerOptions.Signature);

        var sign = new byte[KeyHelper.SignatureLength];
        if (!ecdsa.TrySignData(target, sign.AsSpan(), KeyHelper.HashAlgorithmName, out var written))
        {
            signature = default;
            return false;
        }

        var mics = Mics.GetCorrected();
        signature = new Signature(this.ToPublicKey(), Signature.Type.Attest, mics, sign);
        return true;
    }*/

    public byte[]? SignData(ReadOnlySpan<byte> data)
    {
        var ecdsa = this.TryGetEcdsa();
        if (ecdsa == null)
        {
            return null;
        }

        var sign = new byte[KeyHelper.SignatureLength];
        if (!ecdsa.TrySignData(data, sign.AsSpan(), KeyHelper.HashAlgorithmName, out var written))
        {
            return null;
        }

        return sign;
    }

    public bool SignData(ReadOnlySpan<byte> data, Span<byte> signature, out int written)
    {
        written = 0;
        if (signature.Length < KeyHelper.SignatureLength)
        {
            return false;
        }

        var ecdsa = this.TryGetEcdsa();
        if (ecdsa == null)
        {
            return false;
        }

        if (!ecdsa.TrySignData(data, signature, KeyHelper.HashAlgorithmName, out written))
        {
            return false;
        }

        return true;
    }

    public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> sign)
        => this.ToPublicKey().VerifyData(data, sign);

    #endregion

    #region TypeSpecific

    public SignaturePrivateKey()
    {
    }

    private SignaturePrivateKey(byte[] x, byte[] y, byte[] d)
        : base(KeyClass.T3CS_Signature, x, y, d)
    {
    }

    public SignaturePublicKey ToPublicKey()
        => new(this.keyValue, this.x.AsSpan());

    public override bool Validate()
        => this.KeyClass == KeyClass.T3CS_Signature && base.Validate();

    public bool IsSameKey(SignaturePublicKey publicKey)
        => publicKey.IsSameKey(this);

    public bool Equals(SignaturePrivateKey? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.keyValue == other.keyValue &&
            this.x.AsSpan().SequenceEqual(other.x);
    }

    public override string ToString()
        => this.ToPublicKey().ToString();

    #endregion
}
