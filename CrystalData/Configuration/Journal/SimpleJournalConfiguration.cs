// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
public partial record SimpleJournalConfiguration : JournalConfiguration
{
    public SimpleJournalConfiguration()
        : this(EmptyDirectoryConfiguration.Default)
    {
    }

    public SimpleJournalConfiguration(DirectoryConfiguration configuration)
        : base()
    {
        this.DirectoryConfiguration = configuration;
    }

    [Key(0)]
    public DirectoryConfiguration DirectoryConfiguration { get; protected set; }
}
