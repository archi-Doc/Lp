// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
public partial record S3DirectoryConfiguration : DirectoryConfiguration
{
    public S3DirectoryConfiguration()
        : this(string.Empty, string.Empty)
    {
    }

    public S3DirectoryConfiguration(string bucket, string directory)
        : base(directory)
    {
        this.Bucket = bucket;
    }

    [Key(1)]
    public string Bucket { get; protected set; }

    public override S3FileConfiguration CombinePath(string file)
        => new S3FileConfiguration { Path = System.IO.Path.Combine(this.Path, file), };
}
