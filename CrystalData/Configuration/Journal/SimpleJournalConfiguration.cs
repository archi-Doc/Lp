// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
public partial record SimpleJournalConfiguration : JournalConfiguration
{
    public const int DefaultMaxJournalSizeInMBs = 256; // 256 MB

    public SimpleJournalConfiguration()
        : this(EmptyDirectoryConfiguration.Default, 0)
    {
    }

    public SimpleJournalConfiguration(DirectoryConfiguration configuration, int maxJournalSizeInMBs = DefaultMaxJournalSizeInMBs)
        : base()
    {
        this.DirectoryConfiguration = configuration;

        this.MaxJournalSizeInMBs = maxJournalSizeInMBs;
        if (this.MaxJournalSizeInMBs < DefaultMaxJournalSizeInMBs)
        {
            this.MaxJournalSizeInMBs = DefaultMaxJournalSizeInMBs;
        }
    }

    [Key("Directory")]
    public DirectoryConfiguration DirectoryConfiguration { get; protected set; }

    [Key("JournalSizeInMbs")]
    public int MaxJournalSizeInMBs { get; protected set; }
}
