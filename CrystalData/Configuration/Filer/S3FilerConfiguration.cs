// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record S3FilerConfiguration : FilerConfiguration
{
    public S3FilerConfiguration(string bucket, string directory, string file)
        : base(directory, file)
    {
        this.Bucket = bucket;
    }

    public string Bucket { get; init; }
}
