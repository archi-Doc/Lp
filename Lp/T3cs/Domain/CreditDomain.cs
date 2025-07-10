// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.Net;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial record class CreditDomain
{
    #region FieldAndProperty

    [Key(0)]
    public DomainOption DomainOption { get; init; }

    [Key(1)]
    public Dictionary<SignaturePublicKey, NetNode> Nodes { get; private set; } = new();

    #endregion

    public CreditDomain(DomainOption domainOption)
    {
        this.DomainOption = domainOption;
    }

    public bool TryGetNetNode(SignaturePublicKey publicKey, [MaybeNullWhen(false)] out NetNode netNode)
        => this.Nodes.TryGetValue(publicKey, out netNode);
}
