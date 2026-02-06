// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject(AddAlternateKey = true, EnumAsString = true)]
public partial record class DomainAssignment
{
    #region FieldAndProperty

    [Key(0)]
    // public DomainRole Role { get; init; }
    public string Name { get; init; } = string.Empty;

    [Key(1)]
    public Credit Credit { get; init; } = Credit.UnsafeConstructor();

    [Key(2)]
    public NetNode NetNode { get; init; } = new();

    [Key(3)]
    // public DomainRole Role { get; init; }
    public string Code { get; init; } = string.Empty;

    public ulong GetDomainHash()
        => this.Credit.Identifier.Id1;

    // [Key(2)]
    // [MaxLength(LpConstants.MaxUrlLength)]
    // public partial string Url { get; init; } = string.Empty;

    #endregion

    public DomainAssignment(string name, Credit credit, NetNode netNode, string code)
    {
        this.Name = name;
        this.Credit = credit;
        this.NetNode = netNode;
        this.Code = code;
    }

    public bool Validate()
    {
        return this.Credit.Validate() &&
            this.NetNode.Validate();
    }

    public override string ToString()
        => StringHelper.SerializeToString(this);
}
