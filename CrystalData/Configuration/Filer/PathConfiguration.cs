// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandUnion(0, typeof(EmptyFileConfiguration))]
[TinyhandUnion(1, typeof(EmptyDirectoryConfiguration))]
[TinyhandUnion(2, typeof(LocalFileConfiguration))]
[TinyhandUnion(3, typeof(LocalDirectoryConfiguration))]
[TinyhandUnion(4, typeof(S3FileConfiguration))]
[TinyhandUnion(5, typeof(S3DirectoryConfiguration))]
public abstract partial record PathConfiguration
{
    public enum Type
    {
        Unknown,
        File,
        Directory,
    }

    public PathConfiguration()
        : this(string.Empty)
    {
    }

    public PathConfiguration(string path)
    {
        this.Path = path;
    }

    public virtual Type PathType => Type.Unknown;

    [Key(0)]
    public string Path { get; init; }
}
