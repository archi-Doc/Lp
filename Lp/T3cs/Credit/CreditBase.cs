// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
[ValueLinkObject(Isolation = IsolationLevel.ReadCommitted)]
public partial class CreditPoint : StoragePoint<CreditBase>
{
    [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
    [Key(1)]
    public Credit Credit { get; private set; }

    public CreditPoint(Credit credit)
        : base()
    {
        this.Credit = credit;
    }

    public partial class GoshujinClass
    {
        public ValueTask<CreditBase?> Find(Credit credit)
        {
            return this.TryGet(credit);
        }

        public async Task<DataScope<CreditBase?>> TryLock(Credit credit)
        {
            var result = await this.TryLock(credit, AcquisitionMode.GetOrCreate);
            return result;
        }
    }
}

[TinyhandUnion(0, typeof(EquityCredit))]
public abstract partial record class CreditBase
{
}
