// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Crypto;

#pragma warning disable SA1401

public abstract partial class PrivateKey : IValidatable, IEquatable<PrivateKey>
{
    public PrivateKey()
    {
    }

    protected PrivateKey(KeyClass keyClass, byte[] x, byte[] y, byte[] d)
    {
        this.x = x;
        this.y = y;
        this.d = d;

        var yTilde = KeyHelper.CurveInstance.CompressY(this.y);
        this.keyValue = KeyHelper.CreatePrivateKeyValue(keyClass, yTilde);
    }

    #region FieldAndProperty

    [Key(0)]
    protected readonly byte keyValue;

    [Key(1)]
    protected readonly byte[] x = Array.Empty<byte>();

    [Key(2)]
    protected readonly byte[] y = Array.Empty<byte>();

    [Key(3)]
    protected readonly byte[] d = Array.Empty<byte>();

    public byte KeyValue => this.keyValue;

    public KeyClass KeyClass => KeyHelper.GetKeyClass(this.keyValue);

    public uint YTilde => KeyHelper.GetYTilde(this.keyValue);

    public byte[] X => this.x;

    public byte[] Y => this.y;

    #endregion

    public virtual bool Validate()
    {
        if (this.x == null || this.x.Length != KeyHelper.PublicKeyHalfLength)
        {
            return false;
        }
        else if (this.y == null || this.y.Length != KeyHelper.PublicKeyHalfLength)
        {
            return false;
        }
        else if (this.d == null || this.d.Length != KeyHelper.PrivateKeyLength)
        {
            return false;
        }

        return true;
    }

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

    public string UnsafeToString()
    {
        Span<byte> privateSpan = stackalloc byte[1 + KeyHelper.PrivateKeyLength]; // scoped
        privateSpan[0] = this.keyValue;
        this.d.CopyTo(privateSpan.Slice(1));

        Span<byte> publicSpan = stackalloc byte[1 + KeyHelper.PublicKeyHalfLength];
        publicSpan[0] = KeyHelper.ToPublicKeyValue(this.keyValue);
        this.x.CopyTo(publicSpan.Slice(1));

        return $"!!!{Base64.Url.FromByteArrayToString(privateSpan)}!!!({Base64.Url.FromByteArrayToString(publicSpan)})";
    }
}
