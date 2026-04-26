// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

/// <summary>
/// Represents a full credit entity, including its credit, information, and associated owners.<br/>
/// This class needs to be thread-safe.
/// </summary>
[TinyhandObject(Structural = true)]
public partial record FlatCredit : CreditBase
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
    public MergeableEvidence.GoshujinClass Evidences { get; private set; } = new();

    [Key(4)]
    public AccountableLinkage.GoshujinClass Linkages { get; private set; } = new();

    #endregion

    public FlatCredit()
    {
    }

    public void Initialize(Credit credit, CreditIdentity creditIdentity)
    {
        this.Credit = credit;
        this.CreditIdentity = creditIdentity;
    }
}
