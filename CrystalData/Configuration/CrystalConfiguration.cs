// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public enum SavePolicy
{
    Manual,
    Periodic,
    Instant,
}

public enum SaveFormat
{
    Binary,
    Utf8,
}

[TinyhandObject(ImplicitKeyAsName = true, EnumAsString = true)]
public partial record CrystalConfiguration
{
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(1);

    public static readonly CrystalConfiguration Default = new();

    public CrystalConfiguration()
    {
        this.SavePolicy = SavePolicy.Manual;
        this.FileConfiguration = EmptyFileConfiguration.Default;
        this.StorageConfiguration = EmptyStorageConfiguration.Default;
    }

    public CrystalConfiguration(SavePolicy savePolicy, FileConfiguration fileConfiguration, StorageConfiguration? storageConfiguration = null)
    {
        this.SavePolicy = savePolicy;
        this.SaveInterval = DefaultInterval;
        this.FileConfiguration = fileConfiguration;
        this.StorageConfiguration = storageConfiguration ?? EmptyStorageConfiguration.Default;
    }

    public SaveFormat SaveFormat { get; init; }

    public SavePolicy SavePolicy { get; init; }

    public TimeSpan SaveInterval { get; init; }

    public int NumberOfBackups { get; init; } = 1;

    public FileConfiguration FileConfiguration { get; init; }

    public StorageConfiguration StorageConfiguration { get; init; }
}
