// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
public partial record EmptyDirectoryConfiguration : DirectoryConfiguration
{
    public static readonly EmptyDirectoryConfiguration Default = new();

    public EmptyDirectoryConfiguration()
        : base()
    {
    }

    public override EmptyFileConfiguration CombinePath(string file)
        => new EmptyFileConfiguration { Path = System.IO.Path.Combine(this.Path, file), };

    public override string ToString()
        => $"Empty directory";
}
