// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;
using LP.Obsolete;

namespace LP;

/// <summary>
/// Immutable token object.
/// </summary>
[TinyhandObject]
public sealed partial class Token : IValidatable, IVerifiable // , IEquatable<Token>
{
    public enum TokenType
    {
    }

    public Token()
    {
    }

    public bool Validate()
    {
        if (this.PublicKey.Validate() != true)
        {
            return false;
        }
        else if (this.signature.Length != PublicKey.SignLength)
        {
            return false;
        }

        return true;
    }

    public bool ValidateAndVerify()
    {
        if (!this.Validate())
        {
            return false;
        }

        try
        {
            var result = TinyhandSerializer.SerializeAndGetMarker(this);
            return this.PublicKey.VerifyData(result.ByteArray.AsSpan(0, result.MarkerPosition), this.signature);
        }
        catch
        {
            return false;
        }
    }

    [Key(0)]
    public TokenType Type { get; private set; }

    [Key(1)]
    public long ExpirationMics { get; private set; }

    [Key(2)]
    public PublicKey PublicKey { get; private set; } = default!;

    [Key(3)]
    public Identifier TargetIdentifier { get; private set; }

    [Key(4)]
    public Linkage? TargetLinkage { get; private set; }

    [Key(6, Marker = true, PropertyName = "Signature")]
    [MaxLength(PublicKey.SignLength)]
    private byte[] signature = Array.Empty<byte>();
}
