// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public abstract record FilerConfiguration
{
    public FilerConfiguration(string file)
    {
        // this.Directory = directory;
        this.File = file;
    }

    // public string Directory { get; init; }

    public string File { get; init; }
}
