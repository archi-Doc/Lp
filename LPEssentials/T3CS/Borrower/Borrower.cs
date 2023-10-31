// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;
using ValueLink;

namespace LP.T3CS;

[TinyhandObject(Structual = false)]
[ValueLinkObject(Isolation = IsolationLevel.RepeatableRead)]
public sealed partial record Borrower // : ITinyhandCustomJournal
{
    public Borrower()
    {
    }

    [Key(0)]
    [Link(Unique = true, Primary = true, Type = ChainType.Unordered)]
    private SignaturePublicKey borrowerKey;

    /*
    void ITinyhandCustomJournal.WriteCustomLocator(ref TinyhandWriter writer)
    {
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        return false;
    }*/
}
