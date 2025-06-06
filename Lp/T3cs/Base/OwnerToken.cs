// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Crypto;
using Lp;
using Netsphere.Crypto;
using Netsphere.Misc;
using Tinyhand.IO;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace Lp.T3cs;

/// <summary>
/// Represents an owner authentication token.
/// </summary>
[TinyhandObject]
public sealed partial class OwnerToken : ISignAndVerify, IEquatable<OwnerToken>, IStringConvertible<OwnerToken>
{
    private const char Identifier = 'O';

    static OwnerToken()
    {
        var token = OwnerToken.UnsafeConstructor();
        token.PublicKey = MaxHelper.SignaturePublicKey;
        token.Signature = MaxHelper.Signature;
        token.SignedMics = MaxHelper.Int64;
        token.Salt = MaxHelper.UInt64;
        token.Credit = Credit.Max;
        var rentMemory = TinyhandSerializer.SerializeObjectToRentMemory(token);
        MaxStringLength = Base64.Url.GetEncodedLength(rentMemory.Length) + 10;
        rentMemory.Return();
    }

    public static OwnerToken CreateAndSign(SeedKey seedKey, Connection connection, Credit? credit)
    {
        var token = new OwnerToken();
        token.Credit = credit;
        NetHelper.Sign(seedKey, token, connection);
        return token;
    }

    private OwnerToken()
    {
    }

    public static int MaxStringLength { get; }

    #region FieldAndProperty

    [Key(0)]
    public SignaturePublicKey PublicKey { get; set; }

    [Key(1, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    public byte[] Signature { get; set; } = Array.Empty<byte>();

    [Key(2)]
    public long SignedMics { get; set; }

    [Key(3)]
    public ulong Salt { get; set; }

    [Key(4)]
    public Credit? Credit { get; private set; }

    #endregion

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out OwnerToken instance, out int read, IConversionOptions? conversionOptions = default)
        => TokenHelper.TryParse(Identifier, source, out instance, out read, conversionOptions);

    public bool Validate()
    {
        if (this.SignedMics == 0)
        {
            return false;
        }

        return true;
    }

    public bool Equals(OwnerToken? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.PublicKey.Equals(other.PublicKey) &&
            this.Signature.SequenceEqual(other.Signature) &&
            this.SignedMics == other.SignedMics &&
            this.Salt == other.Salt;
    }

    public override string ToString()
        => TokenHelper.ToBase64(this, Identifier);

    public int GetStringLength()
        => -1;

    public bool TryFormat(Span<char> destination, out int written, IConversionOptions? conversionOptions = default)
        => TokenHelper.TryFormat(this, Identifier, destination, out written, conversionOptions);
}
