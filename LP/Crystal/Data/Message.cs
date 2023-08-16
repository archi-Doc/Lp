// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using ValueLink;

namespace LP.Crystal;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public partial class Message : IIdentifierValidatable<Message>
{
    public const int MaxTitleLength = 100;
    public const int MaxNameLength = 50;
    public const int MaxContentLength = 4_000;

    public enum MessageType
    {
        Standard,
    }

    public Message()
    {
    }

    public Identifier GetIdentifier()
        => this.Identifier;

    public bool ValidateIdentifier()
        => ((IIdentifierValidatable<Message>)this).Validate();

    [Key(0, AddProperty = "Identifier")]
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered, AddValue = false)]
    private Identifier identifier;

    [Key(1, AddProperty = "ParentIdentifier")]
    private Identifier parentIdentifier;

    [Key(3, AddProperty = "Type")]
    private MessageType type;

    [Key(5, AddProperty = "Title")]
    [MaxLength(MaxTitleLength)]
    private string title = default!;

    [Key(6, AddProperty = "Name")]
    [MaxLength(MaxNameLength)]
    private string name = default!;

    [Key(7, AddProperty = "Content")]
    [MaxLength(MaxContentLength)]
    private string content = default!;

    [Key(8, AddProperty = "Signature", Condition = false)]
    private Signature signature = default!;

    [Link(Type = ChainType.Ordered, AddValue = false)]
    public long SignedMics => this.signature.SignedMics;
}
