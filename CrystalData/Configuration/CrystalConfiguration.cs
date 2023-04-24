// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public enum SavePolicy
{
    None,
    Manual,
    Periodic,
    Instant,
}

public enum SaveFormat
{
    Binary,
    Utf8,
}

[TinyhandObject(ImplicitKeyAsName = true, IncludePrivateMembers = true)]
public partial record CrystalConfiguration
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

    public SaveFormat SaveFormat { get; protected set; }

    public SavePolicy SavePolicy { get; protected set; }

    public TimeSpan SaveInterval { get; protected set; }

    public int NumberOfBackups { get; protected set; } = 1;

    public FileConfiguration FileConfiguration { get; protected set; }

    public StorageConfiguration StorageConfiguration { get; protected set; }

    internal void ConfigureInternal(FileConfiguration filerConfiguration, StorageConfiguration storageConfiguration)
    {
        this.FileConfiguration = filerConfiguration;
        this.StorageConfiguration = storageConfiguration;
    }
}
