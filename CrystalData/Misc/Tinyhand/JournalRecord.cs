// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Tinyhand;

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
