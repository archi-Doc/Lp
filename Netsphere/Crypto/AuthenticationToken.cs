// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Crypto;

/// <summary>
/// Represents an authentication token.
/// </summary>
[TinyhandObject]
public partial class AuthenticationToken : ISignAndVerify, IEquatable<AuthenticationToken>
{
    public AuthenticationToken()
    {
    }

    public AuthenticationToken(ulong salt)
    {
        this.Salt = salt;
    }

    #region FieldAndProperty

    [Key(0)]
    public SignaturePublicKey PublicKey { get; set; }

    [Key(1, Level = 1)]
    public byte[] Signature { get; set; } = Array.Empty<byte>();

    [Key(2)]
    public long SignedMics { get; set; }

    [Key(3)]
    public ulong Salt { get; protected set; }

    #endregion

    public bool Validate()
    {
        if (this.SignedMics == 0)
        {
            return false;
        }
        else if (this.Salt == 0)
        {
            return false;
        }

        return true;
    }

    public bool Equals(AuthenticationToken? other)
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
}
