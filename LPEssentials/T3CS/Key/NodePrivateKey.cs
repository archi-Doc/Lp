// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace LP.T3CS;

[TinyhandObject]
public sealed partial class NodePrivateKey : PrivateKey, IEquatable<NodePrivateKey>
{
    #region Unique

    public const string PrivateKeyPath = "NodePrivateKey";

    private static NodePrivateKey? alternativePrivateKey;

    public static NodePrivateKey AlternativePrivateKey
        => alternativePrivateKey ??= NodePrivateKey.Create();

    private static ObjectCache<NodePrivateKey, ECDiffieHellman> PrivateKeyToEcdh { get; } = new(10);

    private ECDiffieHellman? ecdh;

    public static bool TryParse(string base64url, [MaybeNullWhen(false)] out NodePrivateKey privateKey)
    {
        privateKey = null;

        ReadOnlySpan<char> span = base64url.Trim().AsSpan();
        if (!span.StartsWith(KeyHelper.PrivateKeyBrace))
        {// !!!abc
            return false;
        }

        span = span.Slice(KeyHelper.PrivateKeyBrace.Length);
        var bracePosition = span.IndexOf(KeyHelper.PrivateKeyBrace);
        if (bracePosition <= 0)
        {// abc!!!
            return false;
        }

        var privateBytes = Base64.Url.FromStringToByteArray(span.Slice(0, bracePosition));
        if (privateBytes == null || privateBytes.Length != (KeyHelper.PrivateKeyLength + 1))
        {
            return false;
        }

        ECParameters key = default;
        key.Curve = KeyHelper.ECCurve;
        key.D = privateBytes[1..(KeyHelper.PrivateKeyLength + 1)];
        try
        {
            using (var ecdh = ECDiffieHellman.Create(key))
            {
                key = ecdh.ExportParameters(true);
            }
        }
        catch
        {
            return false;
        }

        privateKey = new NodePrivateKey(key.Q.X!, key.Q.Y!, key.D!);
        return true;
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

    internal void CacheEcdh(ECDiffieHellman ecdh)
        => PrivateKeyToEcdh.Cache(this, ecdh);

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

    #endregion
}
