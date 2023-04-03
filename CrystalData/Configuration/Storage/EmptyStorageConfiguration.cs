// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record EmptyStorageConfiguration : StorageConfiguration
{
    public static readonly EmptyStorageConfiguration Default = new();

    public EmptyStorageConfiguration()
        : base()
    {
    }
}
