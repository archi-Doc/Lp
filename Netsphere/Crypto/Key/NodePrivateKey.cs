// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

#pragma warning disable SA1204

namespace Netsphere.Crypto;

/// <summary>
/// Represents a private key data.<br/>
/// Encryption: ECDiffieHellman, secp256r1.
/// </summary>
[TinyhandObject]
public sealed partial class NodePrivateKey : PrivateKeyBase, IEquatable<NodePrivateKey>, IStringConvertible<NodePrivateKey>
{
    #region Unique

    public const string PrivateKeyName = "NodePrivateKey";

    public static readonly NodePrivateKey Empty = new();

    private static NodePrivateKey? alternativePrivateKey;

    public static NodePrivateKey AlternativePrivateKey
        => alternativePrivateKey ??= NodePrivateKey.Create();

    private ECDiffieHellman? ecdh;

    public static int MaxStringLength => UnsafeStringLength;

    public int GetStringLength() => UnsafeStringLength;

    public bool TryFormat(Span<char> destination, out int written)
        => this.UnsafeTryFormat(destination, out written);

    public static bool TryParse(ReadOnlySpan<char> base64url, [MaybeNullWhen(false)] out NodePrivateKey privateKey)
    {
        if (TryParseKey(KeyClass.Node_Encryption, base64url, out var key))
        {
            privateKey = new NodePrivateKey(key.Q.X!, key.Q.Y!, key.D!);
            return true;
        }
        else
        {
            privateKey = default;
            return false;
        }
    }

    public static NodePrivateKey Create()
    {
        var p = KeyHelper.CreateEcdhParameters();
        return new NodePrivateKey(p.Q.X!, p.Q.Y!, p.D!);
    }

    public static NodePrivateKey Create(ReadOnlySpan<byte> seed)
    {
        var p = KeyHelper.CreateEcdhParameters(seed);
        return new NodePrivateKey(p.Q.X!, p.Q.Y!, p.D!);
    }

    public ECDiffieHellman? TryGetEcdh()
    {
        return this.ecdh ??= KeyHelper.CreateEcdhFromD(this.d);
    }

    #endregion

    #region TypeSpecific

    public NodePrivateKey()
    {
    }

    private NodePrivateKey(byte[] x, byte[] y, byte[] d)
        : base(KeyClass.Node_Encryption, x, y, d)
    {
    }

    public NodePublicKey ToPublicKey()
        => new(this.keyValue, this.x.AsSpan());

    public override bool Validate()
        => this.KeyClass == KeyClass.Node_Encryption && base.Validate();

    public bool IsSameKey(NodePublicKey publicKey)
        => publicKey.IsSameKey(this);

    public bool Equals(NodePrivateKey? other)
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
