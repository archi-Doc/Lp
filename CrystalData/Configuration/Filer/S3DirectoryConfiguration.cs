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
    {
        string newPath;
        if (string.IsNullOrEmpty(this.Path))
        {
            newPath = file;
        }
        else if (this.Path.EndsWith('/'))
        {
            newPath = this.Path + file;
        }
        else
        {
            newPath = this.Path + "/" + file;
        }

        return new S3FileConfiguration(this.Bucket, newPath);
    }

    public override string ToString()
        => $"S3 directory: {this.Bucket}/{this.Path}";
}
