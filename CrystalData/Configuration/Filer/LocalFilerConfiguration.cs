// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
public partial record LocalFilerConfiguration : FilerConfiguration
{
    public LocalFilerConfiguration()
    : this(string.Empty)
    {
    }

    public LocalFilerConfiguration(string file)
        : base(file)
    {
    }
}
