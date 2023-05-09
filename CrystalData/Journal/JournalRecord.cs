// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Journal;

public enum JournalType : byte
{
    Waypoint,
    Record,
}

public enum JournalRecord : byte
{
    Locator,
    Key,
    Value,
    Add,
    Remove,
    Clear,
}

/*public readonly ref struct JournalRecord
{
    internal JournalRecord(IJournal journal, byte[] buffer)
    {
        this.Buffer = buffer;
        this.journal = journal;
        this.Writer = new TinyhandWriter(buffer);
    }

    public ulong Close()
    {
        // return this.journal.AddRecord(this);
        return 0;
    }

    public readonly TinyhandWriter Writer;
    internal readonly byte[] Buffer;
    private readonly IJournal journal;
}*/
