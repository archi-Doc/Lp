// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
public partial record LocalDirectoryConfiguration : DirectoryConfiguration
{
    public LocalDirectoryConfiguration()
    : base()
    {
    }

    public LocalDirectoryConfiguration(string directory)
        : base(directory)
    {
    }

    public override LocalFileConfiguration CombinePath(string file)
        => new LocalFileConfiguration(System.IO.Path.Combine(this.Path, file));

    public override string ToString()
        => $"Local directory: {this.Path}";
}
