// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.IO;

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
        => new LocalFileConfiguration { Path = System.IO.Path.Combine(this.Path, file), };
}
