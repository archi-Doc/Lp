// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

#pragma warning disable SA1204

namespace Netsphere.Crypto;

/// <summary>
/// Represents a private key data.<br/>
/// Signature: ECDsa, secp256r1.
/// </summary>
[TinyhandObject]
public sealed partial class SignaturePrivateKey : PrivateKeyBase, IEquatable<SignaturePrivateKey>
{
    #region Unique

    private ECDsa? ecdsa;

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

        var sign = new byte[KeyHelper.SignatureLength];
        if (!ecdsa.TrySignData(target, sign.AsSpan(), KeyHelper.HashAlgorithmName, out var written))
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

    #endregion
}
