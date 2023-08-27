// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using ValueLink;

namespace LP.Crystal;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class Message : ISignatureVerifiable<Message>, IVerifiable
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

    [Key(8, AddProperty = "Signature", Condition = false)]
    private Signature signature = default!;

    [Key(3, AddProperty = "Type")]
    private MessageType type;

    [Key(6, AddProperty = "Name")]
    [MaxLength(MaxNameLength)]
    private string name = default!;

    [Key(5, AddProperty = "Title")]
    [MaxLength(MaxTitleLength)]
    private string title = default!;

    [Key(7, AddProperty = "Content")]
    [MaxLength(MaxContentLength)]
    private string content = default!;

    [Link(Type = ChainType.Ordered, AddValue = false)]
    public long SignedMics => this.signature.SignedMics;

    #endregion

    public Identifier GetIdentifier()
        => this.Identifier;

    public bool VerifyIdentifier()
        => ((IIdentifierVerifiable<Message>)this).VerifyIdentifierDefault();

    public Signature GetSignature()
        => this.Signature;

    public bool VerifySignature()
        => ((ISignatureVerifiable<Message>)this).VerifySignatureDefault();

    public bool ValidateAndVerify()
    {
        if (!this.Validate())
        {
            return false;
        }
        else if (!((ISignatureVerifiable<Message>)this).VerifySignatureDefault())
        {
            return false;
        }

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
