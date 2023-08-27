// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

/// <summary>
/// Immutable token object.
/// </summary>
[TinyhandObject]
public sealed partial class Token // : IVerifiable // , IEquatable<Token>
{
    public const long DefaultMics = Mics.MicsPerSecond * 3;
    public const long MaximumMics = Mics.MicsPerMinute * 2;
    public const long ErrorMics = Mics.MicsPerSecond * 3;

    public static Token Invalid { get; } = new Token(Token.Type.Invalid);

    public enum Type
    {
        Invalid,
        Authorize,
        Identification,
        RequestSummary,
        CreateCredit,
    }

    public Token()
    {
    }

    public Token(Token.Type type, ulong salt, long expirationMics, Identifier targetIdentifier, Linkage? targetLinkage)
    {
        this.TokenType = type;
        this.Salt = salt;
        this.ExpirationMics = expirationMics;
        this.TargetIdentifier = targetIdentifier;
        this.TargetLinkage = targetLinkage;
    }

    internal Token(Token.Type type)
    {
        this.TokenType = type;
    }

    #region FieldAndProperty

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

    [Key(6, AddProperty = "Signature", Condition = false)]
    [MaxLength(PublicKey.SignLength)]
    private byte[] signature = Array.Empty<byte>();

    #endregion

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

        var range = MicsRange.FromCorrectedToMics(MaximumMics, ErrorMics);
        if (!range.IsIn(this.ExpirationMics))
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
            var bytes = TinyhandSerializer.Serialize(this, TinyhandSerializerOptions.Signature);
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

    public bool ValidateAndVerifyWithoutPublicKey()
    {
        if (!this.Validate())
        {
            return false;
        }

        try
        {
            var bytes = TinyhandSerializer.Serialize(this, TinyhandSerializerOptions.Signature);
            return this.PublicKey.VerifyData(bytes, this.signature);
        }
        catch
        {
            return false;
        }
    }

    public bool ValidateAndVerifyWithoutSalt(PublicKey publicKey)
    {
        if (!publicKey.IsValid())
        {
            return false;
        }
        else if (!publicKey.Equals(this.PublicKey))
        {
            return false;
        }

        return this.ValidateAndVerifyWithoutPublicKey();
    }
}
