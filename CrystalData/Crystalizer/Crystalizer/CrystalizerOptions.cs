﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public class CrystalizerOptions
{
    public const int DefaultMemorySizeLimit = 1024 * 1024 * 500; // 500MB
    public const int DefaultMaxParentInMemory = 10_000;

    public CrystalizerOptions()
    {
        this.DefaultTimeout = TimeSpan.MinValue; // TimeSpan.FromSeconds(3);
    }

    public bool EnableFilerLogger { get; set; } = false;

    public string RootPath { get; set; } = string.Empty;

    public TimeSpan DefaultTimeout { get; set; }

    public long MemorySizeLimit { get; set; } = DefaultMemorySizeLimit;

    public int MaxParentInMemory { get; set; } = DefaultMaxParentInMemory;

    public DirectoryConfiguration GlobalDirectory { get; set; } = new LocalDirectoryConfiguration();

    public DirectoryConfiguration? DefaultBackup { get; set; }
}
