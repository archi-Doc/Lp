// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public enum SavePolicy
{
    None,
    Manual,
    Periodic,
    Instant,
}

public record CrystalConfiguration
{
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(1);

    public static readonly CrystalConfiguration Default = new();

    public CrystalConfiguration()
    {
        this.SavePolicy = SavePolicy.None;
        this.FileConfiguration = EmptyFileConfiguration.Default;
        this.StorageConfiguration = EmptyStorageConfiguration.Default;
    }

    public CrystalConfiguration(SavePolicy savePolicy, FileConfiguration filerConfiguration, StorageConfiguration? storageConfiguration = null)
    {
        this.SavePolicy = savePolicy;
        this.SaveInterval = DefaultInterval;
        this.FileConfiguration = filerConfiguration;
        this.StorageConfiguration = storageConfiguration ?? EmptyStorageConfiguration.Default;
    }

    public CrystalConfiguration(TimeSpan interval, FileConfiguration fileConfiguration, StorageConfiguration? storageConfiguration = null)
    {
        this.SavePolicy = SavePolicy.Periodic;
        this.SaveInterval = interval;
        this.FileConfiguration = fileConfiguration;
        this.StorageConfiguration = storageConfiguration ?? EmptyStorageConfiguration.Default;
    }

    public SavePolicy SavePolicy { get; init; }

    public TimeSpan SaveInterval { get; init; }

    public int NumberOfBackups { get; init; } = 1;

    public FileConfiguration FileConfiguration { get; init; }

    public StorageConfiguration StorageConfiguration { get; init; }
}
