// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;

namespace Lp;

[TinyhandObject]
public partial class MergerState : CredentialState
{
    [Key(CredentialState.ReservedKeyCount)]
    public NetNode Node { get; private set; } = new();

    public MergerState(NetNode node)
    {
        this.Node = node;
    }

    protected MergerState()
    {
        this.Node = new();
    }
}
