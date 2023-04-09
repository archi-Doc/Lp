// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandUnion(0, typeof(EmptyJournalConfiguration))]
[TinyhandUnion(1, typeof(SimpleJournalConfiguration))]
public abstract partial record JournalConfiguration
{
    public JournalConfiguration()
    {
    }
}
