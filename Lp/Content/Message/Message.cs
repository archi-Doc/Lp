// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using Tinyhand.IO;

namespace Lp.Crystal;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class Message : IVerifiable, IUnity
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

    [Key(0, AddProperty = "Identifier")]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, AddValue = false)]
    private Identifier identifier;

    [Key(1, AddProperty = "MessageBoardIdentifier")]
    private Identifier messageBoardIdentifier;

    [Key(2, AddProperty = "Signature", Level = TinyhandWriter.DefaultSignatureLevel + 1)]
    private Signature signature = default!;

    // [Key(3, AddProperty = "ValueToken", Level = 2)]
    // private ValueToken valueToken = ValueToken.Default;

    [Key(4, AddProperty = "Type")]
    private MessageType type;

    [Key(5, AddProperty = "Name")]
    [MaxLength(MaxNameLength)]
    private string name = default!;

    [Key(6, AddProperty = "Title")]
    [MaxLength(MaxTitleLength)]
    private string title = default!;

    [Key(7, AddProperty = "Content")]
    [MaxLength(MaxContentLength)]
    private string content = default!;

    // [Link(Type = ChainType.Ordered, AddValue = false)]
    // public long SignedMics => this.valueToken.Signature.SignedMics;

    [IgnoreMember]
    public ulong Hash { get; set; }

    SignaturePublicKey IVerifiable.PublicKey => this.signature.PublicKey;

    byte[] IVerifiable.Signature => this.signature.Sign!;

    #endregion

    public bool ValidateAndVerify()
    {
        if (!this.Validate())
        {
            return false;
        }
        else if (!this.VerifyIdentifierAndSignature(1, this.Identifier, this.Signature))
        {
            return false;
        }

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
        if (this.type == MessageType.Default)
        {
        }
        else
        {
            return false;
        }

        return true;
    }
}
