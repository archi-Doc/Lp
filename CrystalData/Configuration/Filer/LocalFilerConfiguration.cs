// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record LocalFilerConfiguration : FilerConfiguration
{
    public LocalFilerConfiguration(string directory, string file)
        : base(directory, file)
    {
    }

    public LocalFilerConfiguration(string file)
        : base(string.Empty, file)
    {
    }
}
