// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace CrystalData.Journal;

public enum JournalRecordType : byte
{
    SetValue,
    AddObject,
    DeleteObject,
    Waypoint,
    StartSave,
    EndSave,
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
