// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject(Structual = false)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead, Restricted = true)]
public sealed partial record BorrowerData // : ITinyhandCustomJournal
{
    public BorrowerData()
    {
    }

    [Key(0)]
    [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
    public SignaturePublicKey BorrowerPublicKey { get; private set; }

    // [Key(1)]
    // public Proof.GoshujinClass Proofs { get; private set; } = default!;

    [Key(2)]
    public Linkage.GoshujinClass Linkages { get; private set; } = default!;

    /*
    void ITinyhandCustomJournal.WriteCustomLocator(ref TinyhandWriter writer)
    {
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        return false;
    }*/
}
