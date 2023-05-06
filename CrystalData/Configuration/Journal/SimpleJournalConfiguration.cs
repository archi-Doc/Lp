// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
public partial record SimpleJournalConfiguration : JournalConfiguration
{
    public const int DefaultJournalCapacityInMBs = 256; // 256 MB

    public SimpleJournalConfiguration()
        : this(EmptyDirectoryConfiguration.Default, 0)
    {
    }

    public SimpleJournalConfiguration(DirectoryConfiguration configuration, int journalCapacityInMBs = DefaultJournalCapacityInMBs)
        : base()
    {
        this.DirectoryConfiguration = configuration;

        this.JournalCapacityInMBs = journalCapacityInMBs;
        if (this.JournalCapacityInMBs < DefaultJournalCapacityInMBs)
        {
            this.JournalCapacityInMBs = DefaultJournalCapacityInMBs;
        }
    }

    [KeyAsName]
    public DirectoryConfiguration DirectoryConfiguration { get; protected set; }

    [KeyAsName]
    public DirectoryConfiguration? BackupDirectoryConfiguration { get; init; }

    [KeyAsName]
    public int JournalCapacityInMBs { get; protected set; }

    [IgnoreMember]
    public int MaxRecordLength { get; protected set; } = 1024 * 16; // 16 KB

    [IgnoreMember]
    public int FinishedBookLength { get; protected set; } = 1024 * 1024 * 16; // 16 MB

    [IgnoreMember]
    public int MaxMemoryCapacity { get; protected set; } = 1024 * 1024 * 64; // 64 MB
}
