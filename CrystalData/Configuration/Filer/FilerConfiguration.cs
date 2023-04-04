// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandUnion(0, typeof(EmptyFilerConfiguration))]
[TinyhandUnion(1, typeof(LocalFilerConfiguration))]
[TinyhandUnion(2, typeof(S3FilerConfiguration))]
public abstract partial record FilerConfiguration
{
    public FilerConfiguration()
        : this(string.Empty)
    {
    }

    public FilerConfiguration(string file)
    {
        // this.Directory = directory;
        this.File = file;
    }

    // public string Directory { get; init; }

    [Key(0)]
    public string File { get; init; }
}
