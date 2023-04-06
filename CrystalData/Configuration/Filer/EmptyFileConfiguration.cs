// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
public partial record EmptyFileConfiguration : FileConfiguration
{
    public static readonly EmptyFileConfiguration Default = new();

    public EmptyFileConfiguration()
        : base()
    {
    }
}
