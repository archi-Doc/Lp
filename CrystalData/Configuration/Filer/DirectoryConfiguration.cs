// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandUnion("EmptyDirectory", typeof(EmptyDirectoryConfiguration))]
[TinyhandUnion("LocalDirectory", typeof(LocalDirectoryConfiguration))]
[TinyhandUnion("S3Directory", typeof(S3DirectoryConfiguration))]
public abstract partial record DirectoryConfiguration : PathConfiguration
{
    public DirectoryConfiguration()
        : base()
    {
    }

    public DirectoryConfiguration(string directory)
        : base(directory)
    {
    }

    public override Type PathType => Type.Directory;

    public abstract FileConfiguration CombinePath(string file);

    public override string ToString()
        => $"Directory: {this.Path}";
}
