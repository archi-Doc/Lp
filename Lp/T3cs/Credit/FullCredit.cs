// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

#pragma warning disable SA1401

/// <summary>
/// Represents a full credit entity, including its credit, information, and associated owners.<br/>
/// This class needs to be thread-safe.
/// </summary>
[TinyhandObject(Structual = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record FullCredit
{
    #region FieldAndProperty

    /// <summary>
    /// Gets the credit associated with this full credit.<br/>
    /// Thread-safety: Immutable.
    /// </summary>
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    [Key(0)]
    public Credit Credit { get; private set; }

    /// <summary>
    /// Gets the credit information for this full credit.
    /// Thread-safety: Immutable instance.
    /// </summary>
    [Key(1)]
    public CreditInformation CreditInformation { get; private set; } = CreditInformation.UnsafeConstructor();

    /// <summary>
    /// Gets the storage data containing owner data for this full credit.
    /// Thread-safety: Immutable instance.
    /// </summary>
    [Key(2)]
    public StorageData<OwnerData.GoshujinClass> Owners { get; private set; } = new();

    #endregion

    public FullCredit(Credit credit)
    {
        this.Credit = credit;
    }

    /// <summary>
    /// Determines whether the specified <see cref="EvolProof"/> is contained within the owner data of this full credit.
    /// </summary>
    /// <param name="proof">The <see cref="EvolProof"/> to check for existence.</param>
    /// <returns><c>true</c> if the proof exists; otherwise, <c>false</c>.</returns>
    public bool Contains(EvolProof proof)
    {
        var ownerData = this.GetOwnerData(proof.SourceValue.Owner);
        if (ownerData is null)
        {
            return false;
        }

        // ownerData.Linkages.SourceKeyChain.TryGetValue(proof.SourceValue.Owner, out var linkage)
        foreach (var x in ownerData.Linkages.SourceKeyChain.Enumerate(proof.SourceValue.Owner))
        {
            if (x.Proof1 is EvolProof proof2 &&
                proof.ContentEquals(proof2))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the <see cref="OwnerData"/> for the specified owner public key.
    /// </summary>
    /// <param name="ownerPublicKey">The public key of the owner.</param>
    /// <returns>The <see cref="OwnerData"/> if found; otherwise, <c>null</c>.</returns>
    private OwnerData? GetOwnerData(SignaturePublicKey ownerPublicKey)
    {
        return this.Owners.Get().Result.TryGet(ownerPublicKey);
    }
}
