// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record LocalFilerConfiguration : FilerConfiguration
{
    public LocalFilerConfiguration(string path, string file)
        : base(path, file)
    {
    }
}
