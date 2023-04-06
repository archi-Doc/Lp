// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public enum Crystalization
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
        this.Crystalization = Crystalization.None;
        this.FileConfiguration = EmptyFileConfiguration.Default;
        this.StorageConfiguration = EmptyStorageConfiguration.Default;
    }

    public CrystalConfiguration(Crystalization crystalization, FileConfiguration filerConfiguration, StorageConfiguration? storageConfiguration = null)
    {
        this.Crystalization = crystalization;
        this.Interval = DefaultInterval;
        this.FileConfiguration = filerConfiguration;
        this.StorageConfiguration = storageConfiguration ?? EmptyStorageConfiguration.Default;
    }

    public CrystalConfiguration(TimeSpan interval, FileConfiguration fileConfiguration, StorageConfiguration? storageConfiguration = null)
    {
        this.Crystalization = Crystalization.Periodic;
        this.Interval = interval;
        this.FileConfiguration = fileConfiguration;
        this.StorageConfiguration = storageConfiguration ?? EmptyStorageConfiguration.Default;
    }

    public Crystalization Crystalization { get; init; }

    public TimeSpan Interval { get; init; }

    public FileConfiguration FileConfiguration { get; init; }

    public StorageConfiguration StorageConfiguration { get; init; }
}
