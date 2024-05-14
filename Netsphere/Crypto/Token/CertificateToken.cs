// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Misc;

namespace Netsphere.Crypto;

/// <summary>
/// Represents a certificate token.
/// </summary>
/// <typeparam name="T">The type of the certificate object.</typeparam>
[TinyhandObject]
public sealed partial class CertificateToken<T> : ISignAndVerify, IEquatable<CertificateToken<T>>, IStringConvertible<CertificateToken<T>>
    where T : ITinyhandSerialize<T>
{
    private const char Identifier = 'C';

    public CertificateToken()
    {
        this.Target = default!;
    }

    public CertificateToken(T target)
    {
        this.Target = target;
    }

    public static int MaxStringLength => 256;

    #region FieldAndProperty

    [Key(0)]
    public char TokenIdentifier { get; private set; } = Identifier;

    [Key(1)]
    public SignaturePublicKey PublicKey { get; set; }

    [Key(2, Level = 1)]
    public byte[] Signature { get; set; } = Array.Empty<byte>();

    [Key(3)]
    public long SignedMics { get; set; }

    [Key(4)]
    public ulong Salt { get; set; }

    [Key(5)]
    public T Target { get; set; }

    #endregion

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out CertificateToken<T> token)
        => TokenHelper.TryParse(Identifier, source, out token);

    public bool Validate()
    {
        if (this.TokenIdentifier != Identifier)
        {
            return false;
        }
        else if (this.SignedMics == 0)
        {
            return false;
        }

        return true;
    }

    public bool Equals(CertificateToken<T>? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.PublicKey.Equals(other.PublicKey) &&
            this.Signature.SequenceEqual(other.Signature) &&
            this.SignedMics == other.SignedMics;
    }

    public override string ToString()
        => TokenHelper.ToBase64(this, Identifier);

    public int GetStringLength()
        => -1;

    public bool TryFormat(Span<char> destination, out int written)
        => TokenHelper.TryFormat(this, Identifier, destination, out written);
}
