﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject]
public partial record S3FileConfiguration : FileConfiguration
{
    public S3FileConfiguration()
        : this(string.Empty, string.Empty)
    {
    }

    public S3FileConfiguration(string bucket, string file)
        : base(file)
    {
        this.Bucket = bucket;
    }

    [Key("Bucket")]
    public string Bucket { get; protected set; }

    public override string ToString()
        => $"S3 file: {this.Bucket}/{this.Path}";
}
