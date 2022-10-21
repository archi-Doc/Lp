﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

/// <summary>
/// Immutable token object.
/// </summary>
[TinyhandObject]
public sealed partial class Token : IVerifiable // , IEquatable<Token>
{
    public const long DefaultMics = Mics.MicsPerMinute * 1;

    public enum Type
    {
        RequestAuthorization,
        Identification,
        RequestSummary,
    }

    public Token(Token.Type type, ulong salt, long expirationMics, Identifier targetIdentifier, Linkage? targetLinkage)
    {
        this.TokenType = type;
        this.Salt = salt;
        this.ExpirationMics = expirationMics;
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
        this.PublicKey = privateKey.ToPublicKey();

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
    public ulong Salt { get; private set; }

    [Key(2)]
    public long ExpirationMics { get; private set; }

    [Key(3)]
    public PublicKey PublicKey { get; private set; } = default!;

    [Key(4)]
    public Identifier TargetIdentifier { get; private set; }

    [Key(5)]
    public Linkage? TargetLinkage { get; private set; }

    [Key(6, PropertyName = "Signature", Condition = false)]
    [MaxLength(PublicKey.SignLength)]
    private byte[] signature = Array.Empty<byte>();
}
