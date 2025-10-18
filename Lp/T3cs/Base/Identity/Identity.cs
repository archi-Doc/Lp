// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

public enum IdentityKey : int
{
    CreditIdentity,
    MessageIdentity,
}

/// <summary>
/// Represents an abstract base class for identities, providing common properties and validation logic.
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

    /// <summary>
    /// Gets the identifier of the source.
    /// </summary>
    [Key(0)]
    public Identifier SourceIdentifier { get; init; }

    /// <summary>
    /// Gets the public key of the originator.
    /// </summary>
    [Key(1)]
    public SignaturePublicKey Originator { get; init; }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="Identity"/> class.
    /// </summary>
    /// <param name="sourceIdentifier">The identifier of the source.</param>
    /// <param name="originator">The public key of the originator.</param>
    public Identity(Identifier sourceIdentifier, SignaturePublicKey originator)
    {
        this.SourceIdentifier = sourceIdentifier;
        this.Originator = originator;
    }

    /// <summary>
    /// Validates the identity.
    /// </summary>
    /// <returns><c>true</c> if the identity is valid; otherwise, <c>false</c>.</returns>
    public virtual bool Validate()
    {
        if (!this.Originator.Validate())
        {
            return false;
        }

        return true;
    }
}
