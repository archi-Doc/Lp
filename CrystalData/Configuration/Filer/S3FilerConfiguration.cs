// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record S3FilerConfiguration : FilerConfiguration
{
    public S3FilerConfiguration(string bucket, string path, string file)
        : base(path, file)
    {
    }
}
