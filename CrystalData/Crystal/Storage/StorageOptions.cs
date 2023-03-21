// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Data;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class StorageOptions
{
    [SimpleOption("capacity", Description = "Storage capacity in GBs.")]
    public int Capacity { get; set; } = 10;

    [SimpleOption("path", Required = true)]
    public string Path { get; set; } = string.Empty;

    [SimpleOption("bucket")]
    public string Bucket { get; set; } = string.Empty;
}
