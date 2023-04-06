// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandUnion(1, typeof(EmptyDirectoryConfiguration))]
[TinyhandUnion(3, typeof(LocalDirectoryConfiguration))]
[TinyhandUnion(5, typeof(S3DirectoryConfiguration))]
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
}
