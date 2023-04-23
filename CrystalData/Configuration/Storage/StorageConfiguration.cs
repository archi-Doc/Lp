// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandUnion("EmptyStorage", typeof(EmptyStorageConfiguration))]
[TinyhandUnion("SimpleStorage", typeof(SimpleStorageConfiguration))]
public abstract partial record StorageConfiguration
{
    public StorageConfiguration(DirectoryConfiguration filerConfiguration)
    {
        this.DirectoryConfiguration = filerConfiguration;
    }

    [Key("Directory")]
    public DirectoryConfiguration DirectoryConfiguration { get; protected set; }
}
