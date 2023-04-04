// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
public partial record SimpleStorageConfiguration : StorageConfiguration
{
    public SimpleStorageConfiguration(FilerConfiguration filerConfiguration)
        : base(filerConfiguration)
    {
    }
}
