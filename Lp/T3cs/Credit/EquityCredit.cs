// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

#pragma warning disable SA1401

[TinyhandObject]
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
    [Key(0)]
    public Credit Credit { get; private set; } = Credit.UnsafeConstructor();

    /// <summary>
    /// Gets the credit identity.
    /// Thread-safety: Immutable.
    /// </summary>
    [Key(1)]
    public CreditIdentity CreditIdentity { get; private set; } = CreditIdentity.UnsafeConstructor();

    /// <summary>
    /// Gets the credit information.
    /// Thread-safety: Immutable instance.
    /// </summary>
    [Key(2)]
    public CreditInformation CreditInformation { get; private set; } = CreditInformation.UnsafeConstructor();

    [Key(3)]
    private OwnerDataPoint.GoshujinClass owners = new();

    #endregion

    public EquityCredit()
    {
    }

    public void Initialize(Credit credit, CreditIdentity creditIdentity)
    {
        this.Credit = credit;
        this.CreditIdentity = creditIdentity;
    }

    public ValueTask<OwnerData?> GetOwnerData(SignaturePublicKey ownerPublicKey)
    {
        return this.owners.TryGet(ownerPublicKey);
    }

    public OwnerDataPoint? GetOwnerDataPoint(SignaturePublicKey ownerPublicKey)
    {
        var point = this.owners.Find(ownerPublicKey);
        return point;
    }
}
