// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public abstract record FilerConfiguration
{
    public FilerConfiguration(string path, string file)
    {
        this.Path = path;
        this.File = file;
    }

    public string Path { get; init; }

    public string File { get; init; }
}
