// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject(AddAlternateKey = true, EnumAsString = true)]
public partial record class DomainAssignment
{
    #region FieldAndProperty

    [Key(0)]
    public string Name { get; init; } = string.Empty;

    [Key(1)]
    public string Code { get; init; } = string.Empty;

    [Key(2)]
    public CreditIdentity CreditIdentity { get; init; } = CreditIdentity.UnsafeConstructor();

    [Key(3)]
    public NetNode NetNode { get; init; } = new();

    public ulong GetDomainHash()
        => this.CreditIdentity.GetIdentifier().Id1;

    // [Key(2)]
    // [MaxLength(LpConstants.MaxUrlLength)]
    // public partial string Url { get; init; } = string.Empty;

    #endregion

    public DomainAssignment(string name, string code, CreditIdentity creditIdentity, NetNode netNode)
    {
        this.Name = name;
        this.Code = code;
        this.CreditIdentity = creditIdentity;
        this.NetNode = netNode;
    }

    public bool Validate()
    {
        return this.CreditIdentity.Validate() &&
            this.NetNode.Validate();
    }

    public override string ToString()
        => StringHelper.SerializeToString(this);
}
