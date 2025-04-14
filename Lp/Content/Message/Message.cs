// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.Crystal;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class Message // : IVerifiable
{
    public const int MaxTitleLength = 100;
    public const int MaxNameLength = 50;
    public const int MaxContentLength = 4_000;

    public enum MessageType
    {
        Default,
    }

    public Message()
    {
    }

    #region FieldAndProperty

    [Key(0)]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, AddValue = false)]
    public Identifier Identifier { get; private set; }

    [Key(1)]
    public Identifier MessageBoardIdentifier { get; private set; }

    // [Key(2, Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    // public Signature Signature { get; private set; } = default!;

    // [Key(3, Level = 2)]
    // public ValueToken ValueToken { get; private set; } = ValueToken.Default;

    [Key(4)]
    public MessageType Type { get; private set; }

    [Key(5)]
    [MaxLength(MaxNameLength)]
    public partial string Name { get; private set; } = string.Empty;

    [Key(6)]
    [MaxLength(MaxTitleLength)]
    public partial string Title { get; private set; } = string.Empty;

    [Key(7)]
    [MaxLength(MaxContentLength)]
    public partial string Content { get; private set; } = string.Empty;

    // [Link(Type = ChainType.Ordered, AddValue = false)]
    // public long SignedMics => this.valueToken.Signature.SignedMics;

    [IgnoreMember]
    public ulong Hash { get; set; }

    // SignaturePublicKey2 IVerifiable.PublicKey => this.signature.PublicKey;

    // byte[] IVerifiable.Signature => this.signature.Sign!;

    #endregion

    public bool ValidateAndVerify()
    {
        if (!this.Validate())
        {
            return false;
        }

        /*else if (!this.VerifyIdentifierAndSignature(1, this.Identifier, this.Signature))
        {
            return false;
        }*/

        /*else if (this.ValueToken.Signature.SignatureType != Signature.Type.Attest)
        {
            return false;
        }
        else if (!this.VerifyValueToken(3, this.ValueToken))
        {
            return false;
        }*/

        return true;
    }

    public bool Validate()
    {
        if (this.Type == MessageType.Default)
        {
        }
        else
        {
            return false;
        }

        return true;
    }
}
