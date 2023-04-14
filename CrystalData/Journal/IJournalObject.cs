// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.IO;

namespace CrystalData.Journal;

public interface IJournalObject
{
    void ReadJournal(ref TinyhandReader reader);
}
