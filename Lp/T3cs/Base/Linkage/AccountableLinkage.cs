// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Netsphere.Crypto;

namespace Lp.T3cs;

[ValueLinkObject]
[TinyhandObject]
public partial class AccountableLinkage : Linkage
{
    public static bool TryCreate(ContractableEvidence evidence1, ContractableEvidence evidence2, [MaybeNullWhen(false)] out AccountableLinkage linkage)
        => TryCreate(() => new(), evidence1, evidence2, out linkage);

    [Link(Primary = true, Unique = true, Type = ChainType.Ordered, TargetMember = nameof(LinkedMics))]
    protected AccountableLinkage()
    {
    }

    [Link(Type = ChainType.Unordered)]
    public SignaturePublicKey SourceKey => this.Proof1.GetSignatureKey();
}
