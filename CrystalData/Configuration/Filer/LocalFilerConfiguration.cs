// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record LocalFilerConfiguration : FilerConfiguration
{
    public LocalFilerConfiguration(string file)
        : base(file)
    {
    }

    public int ConcurrentTasks { get; init; }
}
