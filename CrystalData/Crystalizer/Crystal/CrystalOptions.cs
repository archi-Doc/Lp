// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record CrystalOptions
{
    public const long DefaultDirectoryCapacity = 1024L * 1024 * 1024 * 10; // 10GB
    public const int DefaultMaxDataSize = 1024 * 1024 * 4; // 4MB
    public const int DefaultMaxFragmentSize = 1024 * 4; // 4KB
    public const int DefaultMaxFragmentCount = 1000;
    public const int DefaultMemorySizeLimit = 1024 * 1024 * 500; // 500MB
    public const int DefaultMaxParentInMemory = 10_000;

    public static CrystalOptions Default { get; } = new CrystalOptions();

    public CrystalOptions()
        : this(new Dictionary<Type, CrystalConfiguration>(), new Dictionary<Type, BigCrystalConfiguration>(), string.Empty)
    {
    }

    internal CrystalOptions(Dictionary<Type, CrystalConfiguration> crystalConfigurations, Dictionary<Type, BigCrystalConfiguration> bigCrystalConfigurations, string crystalDirectory)
    {
        this.CrystalConfigurations = crystalConfigurations;
        this.BigCrystalConfigurations = bigCrystalConfigurations;
        this.CrystalDirectory = crystalDirectory;
    }

    public Dictionary<Type, CrystalConfiguration> CrystalConfigurations { get; }

    public Dictionary<Type, BigCrystalConfiguration> BigCrystalConfigurations { get; }

    /// <summary>
    /// Gets or sets a path of the directory where data files are stored.
    /// </summary>
    public string CrystalDirectory { get; set; }

    public int MaxDataSize { get; init; } = DefaultMaxDataSize;

    public int MaxFragmentSize { get; init; } = DefaultMaxFragmentSize;

    public int MaxFragmentCount { get; init; } = DefaultMaxFragmentCount;

    public long MemorySizeLimit { get; init; } = DefaultMemorySizeLimit;

    public long DirectoryCapacity { get; init; } = 1024L * 1024 * 1024 * 10; // 10GB

    public int MaxParentInMemory { get; set; } = DefaultMaxParentInMemory;

    public string CrystalFile { get; init; } = "Crystal.main";

    public string CrystalBackup { get; init; } = "Crystal.back";

    public string StorageFile { get; init; } = "Storage.main";

    public string StorageBackup { get; init; } = "Storage.back";

    public string DefaultCrystalDirectory { get; init; } = "Crystal";

    public bool EnableLogger { get; init; } = true;

    public string RootPath => this.rootPath ??= PathHelper.GetRootedDirectory(Directory.GetCurrentDirectory(), this.CrystalDirectory);

    public string CrystalFilePath => PathHelper.GetRootedFile(this.RootPath, this.CrystalFile);

    public string CrystalBackupPath => PathHelper.GetRootedFile(this.RootPath, this.CrystalBackup);

    public string StorageFilePath => PathHelper.GetRootedFile(this.RootPath, this.StorageFile);

    public string StorageBackupPath => PathHelper.GetRootedFile(this.RootPath, this.StorageBackup);

    private string? rootPath;
}
