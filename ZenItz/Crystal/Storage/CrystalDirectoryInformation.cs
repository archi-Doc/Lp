// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record CrystalDirectoryInformation(uint DirectoryId, CrystalDirectoryType Type, string DirectoryPath, long DirectoryCapacity, long DirectorySize, double UsageRatio)
{
    public override string ToString()
        => $"Id: {this.DirectoryId.To4Hex()}, Path: {this.DirectoryPath}, Size/Capacity: {this.DirectorySize}/{this.DirectoryCapacity} ({$"{this.UsageRatio,0:F1}"})";
}
