// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

public enum IdentityKey : int
{
    CreditIdentity,
    MessageIdentity,
}

/// <summary>
/// Represents a credit identity.
/// </summary>
[TinyhandUnion((int)IdentityKey.CreditIdentity, typeof(CreditIdentity))]
[TinyhandUnion((int)IdentityKey.MessageIdentity, typeof(MessageIdentity))]
[TinyhandObject(ReservedKeyCount = ReservedKeyCount)]
public abstract partial record class Identity : IValidatable
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public const int ReservedKeyCount = 3;

    #region FieldAndProperty

    // [Key(0)]
    // public IdentityKind Kind { get; init; }

    [Key(0)]
    public Identifier SourceIdentifier { get; init; }

    [Key(1)]
    public SignaturePublicKey Originator { get; init; }

    #endregion

    // [SetsRequiredMembers]
    public Identity(Identifier sourceIdentifier, SignaturePublicKey originator)
    {
        this.SourceIdentifier = sourceIdentifier;
        this.Originator = originator;
    }

    public virtual bool Validate()
    {
        if (!this.Originator.Validate())
        {
            return false;
        }

        /*if (this.Mergers.Length == 0 ||
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
        }*/

        return true;
    }
}
