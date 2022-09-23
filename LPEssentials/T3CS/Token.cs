// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;
using LP.Obsolete;

namespace LP;

/// <summary>
/// Immutable token object.
/// </summary>
[TinyhandObject]
public sealed partial class Token : IValidatable // , IEquatable<Token>
{
    public Token()
    {
    }

    public bool Validate()
    {
        if (this.Authority?.Validate() != true)
        {
            return false;
        }
        else if (this.sign.Length != LP.Authority.SignLength)
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
            return this.Authority.VerifyData(result.ByteArray.AsSpan(0, result.MarkerPosition), this.sign);
        }
        catch
        {
            return false;
        }
    }

    [Key(0)]
    public long ExpirationMics { get; private set; }

    [Key(1)]
    public AuthorityPublicKey Authority { get; private set; } = default!;

    [Key(2)]
    public Identifier TargetIdentifier { get; private set; }

    [Key(3)]
    public Linkage? TargetLinkage { get; private set; }

    [Key(5, Marker = true, PropertyName = "Sign")]
    [MaxLength(LP.Authority.SignLength)]
    private byte[] sign = Array.Empty<byte>();
}
