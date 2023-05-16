// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
public partial record RelativeFileConfiguration : FileConfiguration
{
    public RelativeFileConfiguration()
        : base()
    {
    }

    public RelativeFileConfiguration(string file)
        : base(file)
    {
    }

    public override RelativeFileConfiguration AppendPath(string file)
        => new RelativeFileConfiguration(this.Path + file);

    public override string ToString()
        => $"Relative file: {this.Path}";
}
