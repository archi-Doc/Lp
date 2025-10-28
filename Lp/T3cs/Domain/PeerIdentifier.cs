// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial record class PeerIdentifier
{
    #region FieldAndProperty

    [Key(0)]
    [MaxLength(LpConstants.MaxCodeLength)]
    public partial string Code { get; init; } = string.Empty;

    [Key(1)]
    public Credit Credit { get; init; } = Credit.UnsafeConstructor();

    [Key(2)]
    public NetNode NetNode { get; init; } = new();

    [Key(3)]
    [MaxLength(LpConstants.MaxUrlLength)]
    public partial string Url { get; init; } = string.Empty;

    #endregion

    public PeerIdentifier(string code, Credit credit, NetNode netNode, string url)
    {
        this.Code = code;
        this.Credit = credit;
        this.NetNode = netNode;
        this.Url = url;
    }

    public bool Validate()
    {
        return this.Code.Length <= LpConstants.MaxCodeLength &&
            this.Credit.Validate() &&
            this.NetNode.Validate();
    }
}
