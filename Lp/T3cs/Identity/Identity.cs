// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

public enum IdentityKey : int
{
    CreditIdentity,
    BoardIdentity,
}

/// <summary>
/// Represents a credit identity.
/// </summary>
// [TinyhandUnion((int)IdentityKey.CreditIdentity, typeof(CreditIdentity))]
// [TinyhandUnion((int)IdentityKey.BoardIdentity, typeof(BoardIdentity))]
[TinyhandObject(ReservedKeyCount = ReservedKeyCount)]
public  partial class Identity : IValidatable
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public const int ReservedKeyCount = 4;

    #region FieldAndProperty

    [Key(0)]
    public required IdentityKind Kind { get; init; }

    [Key(1)]
    public required Identifier SourceIdentifier { get; init; }

    [Key(2)]
    public required SignaturePublicKey Originator { get; init; }

    [Key(3)]
    [MaxLength(LpConstants.MaxMergers)]
    public required partial SignaturePublicKey[] Mergers { get; init; } = [];

    #endregion

    public Identity()
    {
    }

    [SetsRequiredMembers]
    public Identity(IdentityKind identityKind, SignaturePublicKey originator, SignaturePublicKey[] mergers)
    {
        this.SourceIdentifier = default;
        this.Originator = originator;
        this.Mergers = mergers;
        this.Kind = identityKind;
    }

    public bool Validate()
    {
        if (!this.SourceIdentifier.IsValid)
        {
            return false;
        }

        if (!this.Originator.Validate())
        {
            return false;
        }

        if (this.Mergers.Length == 0 ||
            this.Mergers.Length > LpConstants.MaxMergers)
        {
            return false;
        }

        foreach (var x in this.Mergers)
        {
            if (!x.Validate())
            {
                return false;
            }
        }

        return true;
    }
}
