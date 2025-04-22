// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

/// <summary>
/// Represents a credit identity.
/// </summary>
[TinyhandObject]
public sealed partial record CreditIdentity : IValidatable
{// [property: Key(4)] string Name
    #region FieldAndProperty

    [Key(0)]
    public required Identifier SourceIdentifier { get; init; }

    [Key(1)]
    public SignaturePublicKey Originator { get; private set; }

    [Key(2)]
    [MaxLength(Credit.MaxMergers)]
    public partial SignaturePublicKey[] Mergers { get; private set; } = [];

    [Key(3)]
    public CreditKind Kind { get; private set; }

    public int MergerCount => this.Mergers.Length;

    #endregion

    /*[SetsRequiredMembers]
    public CreditIdentity()
    {
    }*/

    public bool Validate()
    {
        if (!this.SourceIdentifier.IsDefault())
        {
            return false;
        }

        if (!this.Originator.Validate())
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
