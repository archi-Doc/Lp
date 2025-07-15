// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

#pragma warning disable SA1401

[TinyhandObject(Structual = true)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public partial record FullCredit
{
    #region FieldAndProperty

    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    [Key(0)]
    public Credit Credit { get; protected set; } = Credit.UnsafeConstructor();

    [Key(1)]
    public CreditInformation CreditInformation { get; protected set; } = CreditInformation.UnsafeConstructor();

    [Key(2)]
    public StorageData<OwnerData.GoshujinClass> Owners { get; protected set; } = new();

    #endregion

    public FullCredit()
    {
    }

    public bool Contains(EvolProof proof)
    {
        var ownerData = this.Find(proof.SourceValue.Owner);
    }

    private OwnerData? Find(SignaturePublicKey ownerPublicKey)
    {
        return this.Owners.Get().Result.TryGet(ownerPublicKey);
    }
}
