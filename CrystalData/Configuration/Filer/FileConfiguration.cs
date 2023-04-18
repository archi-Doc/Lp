﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandUnion("EmptyFile", typeof(EmptyFileConfiguration))]
[TinyhandUnion("LocalFile", typeof(LocalFileConfiguration))]
[TinyhandUnion("S3File", typeof(S3FileConfiguration))]
public abstract partial record FileConfiguration : PathConfiguration, IEquatable<FileConfiguration>
{
    public FileConfiguration()
        : base()
    {
    }

    public FileConfiguration(string file)
        : base(file)
    {
    }

    public override Type PathType => Type.File;

    public override string ToString()
        => $"File: {this.Path}";
}
