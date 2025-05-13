// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

/// <summary>
/// Represents a credit identity.
/// </summary>
[TinyhandObject(ReservedKeyCount = ReservedKeyCount)]
public partial record Identity : IValidatable
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
    [MaxLength(Credit.MaxMergers)]
    public required partial SignaturePublicKey[] Mergers { get; init; } = [];

    #endregion

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
            this.Mergers.Length > Credit.MaxMergers)
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
