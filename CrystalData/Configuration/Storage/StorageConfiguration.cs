// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandUnion(0, typeof(EmptyStorageConfiguration))]
[TinyhandUnion(1, typeof(SimpleStorageConfiguration))]
public abstract partial record StorageConfiguration
{
    public StorageConfiguration(DirectoryConfiguration filerConfiguration)
    {
        this.DirectoryConfiguration = filerConfiguration;
    }

    [Key(0)]
    public DirectoryConfiguration DirectoryConfiguration { get; private set; }
}
