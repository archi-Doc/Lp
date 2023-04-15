// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public enum SaveMethod
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
        this.Save = SaveMethod.None;
        this.FileConfiguration = EmptyFileConfiguration.Default;
        this.StorageConfiguration = EmptyStorageConfiguration.Default;
    }

    public CrystalConfiguration(SaveMethod saveMethod, FileConfiguration filerConfiguration, StorageConfiguration? storageConfiguration = null)
    {
        this.Save = saveMethod;
        this.Interval = DefaultInterval;
        this.FileConfiguration = filerConfiguration;
        this.StorageConfiguration = storageConfiguration ?? EmptyStorageConfiguration.Default;
    }

    public CrystalConfiguration(TimeSpan interval, FileConfiguration fileConfiguration, StorageConfiguration? storageConfiguration = null)
    {
        this.Save = SaveMethod.Periodic;
        this.Interval = interval;
        this.FileConfiguration = fileConfiguration;
        this.StorageConfiguration = storageConfiguration ?? EmptyStorageConfiguration.Default;
    }

    public SaveMethod Save { get; init; }

    public TimeSpan Interval { get; init; }

    public FileConfiguration FileConfiguration { get; init; }

    public StorageConfiguration StorageConfiguration { get; init; }

    public bool AddDefaultExtension { get; init; } = true;
}
