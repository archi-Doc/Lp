// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Journal;

[ValueLinkObject]
internal partial class SimpleJournalBook
{
    [Link(Type = ChainType.LinkedList, Name = "OnMemory", AutoLink = false)]
    public SimpleJournalBook()
    {
    }

    [Link(Type = ChainType.Ordered)]
    private ulong start;

    private ulong length;
}
