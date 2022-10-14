// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;
using LP.Obsolete;

namespace LP;

/// <summary>
/// Immutable token object.
/// </summary>
[TinyhandObject]
public sealed partial class Token : IVerifiable // , IEquatable<Token>
{
    public enum Type
    {
        Identification,
        RequestSummary,
    }

    public Token(Token.Type type, long expirationMics, PublicKey publicKey, Identifier targetIdentifier, Linkage? targetLinkage)
    {
        this.TokenType = type;
        this.ExpirationMics = expirationMics;
        this.PublicKey = publicKey;
        this.TargetIdentifier = targetIdentifier;
        this.TargetLinkage = targetLinkage;
    }

    internal Token()
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

    public bool Sign(PrivateKey privateKey)
    {
        if (!this.PublicKey.IsSameKey(privateKey))
        {
            return false;
        }

        try
        {
            var bytes = TinyhandSerializer.Serialize(this, TinyhandSerializerOptions.Conditional);
            var sign = privateKey.SignData(bytes);

            if (sign != null)
            {
                this.signature = sign;
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    public bool ValidateAndVerify()
    {
        if (!this.Validate())
        {
            return false;
        }

        try
        {
            var bytes = TinyhandSerializer.Serialize(this, TinyhandSerializerOptions.Conditional);
            return this.PublicKey.VerifyData(bytes, this.signature);
        }
        catch
        {
            return false;
        }
    }

    [Key(0)]
    public Type TokenType { get; private set; }

    [Key(1)]
    public long ExpirationMics { get; private set; }

    [Key(2)]
    public PublicKey PublicKey { get; private set; } = default!;

    [Key(3)]
    public Identifier TargetIdentifier { get; private set; }

    [Key(4)]
    public Linkage? TargetLinkage { get; private set; }

    [Key(6, PropertyName = "Signature", Condition = false)]
    [MaxLength(PublicKey.SignLength)]
    private byte[] signature = Array.Empty<byte>();
}
