﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Lp.T3cs;

[ValueLinkObject]
[TinyhandObject]
public partial class MergeableLinkage : Linkage
{
    public static bool TryCreate(ContractableEvidence evidence1, ContractableEvidence evidence2, [MaybeNullWhen(false)] out MergeableLinkage linkage)
        => TryCreate(() => new(), evidence1, evidence2, out linkage);

    [Link(Primary = true, Unique = true, Type = ChainType.Ordered, TargetMember = nameof(LinkedMics))]
    protected MergeableLinkage()
    {
    }
}
