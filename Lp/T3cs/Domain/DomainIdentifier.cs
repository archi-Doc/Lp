// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[TinyhandObject(AddAlternateKey = true)]
public partial record class DomainNode
{
    #region FieldAndProperty

    [Key(0)]
    public Credit Credit { get; init; } = Credit.UnsafeConstructor();

    [Key(1)]
    public NetNode NetNode { get; init; } = new();

    // [Key(2)]
    // [MaxLength(LpConstants.MaxUrlLength)]
    // public partial string Url { get; init; } = string.Empty;

    #endregion

    public DomainNode(Credit credit, NetNode netNode)
    {
        this.Credit = credit;
        this.NetNode = netNode;
    }

    public bool Validate()
    {
        return this.Credit.Validate() &&
            this.NetNode.Validate();
    }
}
