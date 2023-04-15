// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record BigCrystalConfiguration : CrystalConfiguration
{
    public const string DefaultCrystalDirectory = "Crystal";
    public const int DefaultMaxDataSize = 1024 * 1024 * 4; // 4MB
    public const int DefaultMaxFragmentSize = 1024 * 4; // 4KB
    public const int DefaultMaxFragmentCount = 1000;
    public const int DefaultMemorySizeLimit = 1024 * 1024 * 500; // 500MB
    public const int DefaultMaxParentInMemory = 10_000;

    public static new readonly BigCrystalConfiguration Default = new BigCrystalConfiguration();

    public BigCrystalConfiguration()
    {
        this.DirectoryConfiguration = EmptyDirectoryConfiguration.Default;
        this.StorageConfiguration = EmptyStorageConfiguration.Default;
        this.RegisterDatum = registry => { };
    }

    public BigCrystalConfiguration(Action<DatumRegistry> registerDatum, SaveMethod saveMethod, DirectoryConfiguration directoryConfiguration, StorageConfiguration storageConfiguration)
        : base(saveMethod, EmptyFileConfiguration.Default)
    {
        this.RegisterDatum = registerDatum;
        this.DirectoryConfiguration = directoryConfiguration;
        this.StorageConfiguration = storageConfiguration;
    }

    public DirectoryConfiguration DirectoryConfiguration { get; init; }

    public Action<DatumRegistry> RegisterDatum { get; init; }

    public int MaxDataSize { get; init; } = DefaultMaxDataSize;

    public int MaxFragmentSize { get; init; } = DefaultMaxFragmentSize;

    public int MaxFragmentCount { get; init; } = DefaultMaxFragmentCount;

    public long MemorySizeLimit { get; init; } = DefaultMemorySizeLimit;

    public int MaxParentInMemory { get; set; } = DefaultMaxParentInMemory;

    public string CrystalFile { get; init; } = "Crystal";

    public string StorageFile { get; init; } = "Storage";
}
