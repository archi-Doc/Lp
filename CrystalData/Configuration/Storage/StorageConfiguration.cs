// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandUnion("EmptyStorage", typeof(EmptyStorageConfiguration))]
[TinyhandUnion("SimpleStorage", typeof(SimpleStorageConfiguration))]
public abstract partial record StorageConfiguration
{
    public StorageConfiguration(DirectoryConfiguration directoryConfiguration, DirectoryConfiguration? backupDirectoryConfiguration = null)
    {
        this.DirectoryConfiguration = directoryConfiguration;
        this.BackupDirectoryConfiguration = backupDirectoryConfiguration;
    }

    [Key("Directory")]
    public DirectoryConfiguration DirectoryConfiguration { get; protected set; }

    [Key("BackupDirectory")]
    public DirectoryConfiguration? BackupDirectoryConfiguration { get; protected set; }
}
