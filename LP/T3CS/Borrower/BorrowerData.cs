// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace LP.T3CS;

[TinyhandObject(Structual = false)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead, Restricted = true)]
public sealed partial record BorrowerData // : ITinyhandCustomJournal
{
    public BorrowerData()
    {
    }

    [Key(0)]
    [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
    private SignaturePublicKey borrowerKey;

    // [Key(1)]
    // public Proof.GoshujinClass proofs { get; private set; }

    /*
    void ITinyhandCustomJournal.WriteCustomLocator(ref TinyhandWriter writer)
    {
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        return false;
    }*/
}
