// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

#pragma warning disable SA1401

[TinyhandObject(Structural = true)]
[ValueLinkObject(Isolation = IsolationLevel.ReadCommitted)]
public partial class EquityCreditPoint : StoragePoint<EquityCredit>
{
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    [Key(1)]
    public Credit Credit { get; private set; }

    public EquityCreditPoint(Credit credit)
        : base()
    {
        this.Credit = credit;
    }
}

/// <summary>
/// Represents a full credit entity, including its credit, information, and associated owners.<br/>
/// This class needs to be thread-safe.
/// </summary>
[TinyhandObject(Structural = true)]
public partial record EquityCredit
{
    #region FieldAndProperty

    /// <summary>
    /// Gets the credit associated with this equity credit.<br/>
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

    [Key(2)]
    public StoragePoint<OwnerData.GoshujinClass> Owners { get; private set; } = new();

    #endregion

    public EquityCredit(Credit credit)
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
    {//
        var ownerData = this.Owners.TryGet().Result;
        if (ownerData is null)
        {
            return default;
        }

        return ownerData.TryGet(ownerPublicKey);
    }
}
