// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

/*
/// <summary>
/// Immutable linkage object.
/// </summary>
[TinyhandObject]
public sealed partial class Linkage : IValidatable, IEquatable<Linkage>
{
    public const int MaxOwners = 2;
    public const int MaxStringLength = 32;

    public Linkage()
    {
    }

    [Key(0)]
    public Identifier LinkageId { get; private set; }

    [Key(1, AddProperty = "LeftOwners")]
    [MaxLength(MaxOwners)]
    private SignaturePublicKey[] leftOwners = default!;

    [Key(2)]
    public Value? LeftValue { get; private set; } = default!;

    [Key(3, AddProperty = "LeftString")]
    [MaxLength(MaxStringLength)]
    private string leftString = string.Empty;

    [Key(4, AddProperty = "RightOwners")]
    [MaxLength(MaxOwners)]
    private SignaturePublicKey[] rightOwners = default!;

    [Key(5)]
    public Value? RightValue { get; private set; } = default!;

    [Key(6, AddProperty = "RightString")]
    [MaxLength(MaxStringLength)]
    private string rightString = string.Empty;

    [Key(7, AddProperty = "Signatures", Level = 0)]
    [MaxLength((MaxOwners + 1 + Credit.MaxMergers) * 2)]
    private byte[] signatures = default!;

    public bool Equals(Linkage? other)
    {
        throw new NotImplementedException();
    }

    public bool Validate()
    {
        throw new NotImplementedException();
    }
}
*/
