// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject(AddAlternateKey = true, EnumAsString = true)]
public partial record class DomainAssignment
{
    #region FieldAndProperty

    [Key(0)]
    public DomainRole Role { get; init; }

    [Key(1)]
    public Credit Credit { get; init; } = Credit.UnsafeConstructor();

    [Key(2)]
    public NetNode NetNode { get; init; } = new();

    // [Key(2)]
    // [MaxLength(LpConstants.MaxUrlLength)]
    // public partial string Url { get; init; } = string.Empty;

    #endregion

    public DomainAssignment(DomainRole role, Credit credit, NetNode netNode)
    {
        this.Role = role;
        this.Credit = credit;
        this.NetNode = netNode;
    }

    public bool Validate()
    {
        return this.Credit.Validate() &&
            this.NetNode.Validate();
    }
}
