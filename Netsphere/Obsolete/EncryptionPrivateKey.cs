// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

#pragma warning disable SA1204

namespace Netsphere.Crypto;

/*/// <summary>
/// Represents a private key data.<br/>
/// Encryption: ECDiffieHellman, secp256r1.
/// </summary>
[TinyhandObject]
public sealed partial class EncryptionPrivateKey : PrivateKeyBase, IEquatable<EncryptionPrivateKey>, IStringConvertible<EncryptionPrivateKey>
{
    #region Unique

    public static readonly EncryptionPrivateKey Empty = new();

    private ECDiffieHellman? ecdh;

    public static int MaxStringLength => UnsafeStringLength;

    public int GetStringLength() => UnsafeStringLength;

    public bool TryFormat(Span<char> destination, out int written)
        => this.UnsafeTryFormat(destination, out written);

    public static bool TryParse(ReadOnlySpan<char> base64url, [MaybeNullWhen(false)] out EncryptionPrivateKey privateKey)
    {
        if (TryParseKey(KeyClass.Encryption, base64url, out var key))
        {
            privateKey = new EncryptionPrivateKey(key.Q.X!, key.Q.Y!, key.D!);
            return true;
        }
        else
        {
            privateKey = default;
            return false;
        }
    }

    public static EncryptionPrivateKey Create()
    {
        var p = KeyHelper.CreateEcdhParameters();
        return new EncryptionPrivateKey(p.Q.X!, p.Q.Y!, p.D!);
    }

    public static EncryptionPrivateKey Create(ReadOnlySpan<byte> seed)
    {
        var p = KeyHelper.CreateEcdhParameters(seed);
        return new EncryptionPrivateKey(p.Q.X!, p.Q.Y!, p.D!);
    }

    public ECDiffieHellman? TryGetEcdh()
    {
        return this.ecdh ??= KeyHelper.CreateEcdhFromD(this.d);
    }

    #endregion

    #region TypeSpecific

    public EncryptionPrivateKey()
    {
    }

    private EncryptionPrivateKey(byte[] x, byte[] y, byte[] d)
        : base(KeyClass.Encryption, x, y, d)
    {
    }

    public EncryptionPublicKey ToPublicKey()
        => new(this.keyValue, this.x.AsSpan());

    public override bool Validate()
        => this.KeyClass == KeyClass.Encryption && base.Validate();

    public bool IsSameKey(EncryptionPublicKey publicKey)
        => publicKey.IsSameKey(this);

    public bool Equals(EncryptionPrivateKey? other)
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
}*/
