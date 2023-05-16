// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
public partial record RelativeDirectoryConfiguration : DirectoryConfiguration
{
    public RelativeDirectoryConfiguration()
    : base()
    {
    }

    public RelativeDirectoryConfiguration(string directory)
        : base(directory)
    {
    }

    public override RelativeFileConfiguration CombineFile(string file)
        => new RelativeFileConfiguration(PathHelper.CombineWithSlash(this.Path, PathHelper.GetPathNotRoot(file)));

    public override RelativeDirectoryConfiguration CombineDirectory(DirectoryConfiguration directory)
        => new RelativeDirectoryConfiguration(PathHelper.CombineWithSlash(this.Path, PathHelper.GetPathNotRoot(directory.Path)));

    public override string ToString()
        => $"Relative directory: {this.Path}";
}
