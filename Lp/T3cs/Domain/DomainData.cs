// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject]
public partial record class DomainData
{
    public const string Filename = "DomainData";
    public const int MaxNodeCount = 1_000; // Maximum number of nodes in the domain data.

    #region FieldAndProperty

    [Key(0)]
    public Credit Credit { get; private set; } = Credit.UnsafeConstructor();

    [Key(1)]
    public NodeProof.GoshujinClass Nodes { get; private set; } = new();

    [Key(2)]
    public CreditEvols.GoshujinClass CreditEvols { get; private set; } = new();

    [Key(3)]
    public byte[] DomainSignature { get; private set; } = [];

    [Key(4)]
    public byte[] DomainEvols { get; private set; } = [];

    #endregion

    public DomainData()
    {
    }

    public void SetCredit(Credit credit)
    {
        if (!credit.Equals(this.Credit))
        {
            this.Credit = credit;
            this.Clear();
        }
    }

    public void Clear()
    {
        this.Nodes.Clear();
        this.CreditEvols.Clear();
        this.DomainSignature = [];
        this.DomainEvols = [];
    }
}
