// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record BigCrystalOptions
{
    public const string DefaultCrystalDirectory = "Crystal";
    public const int DefaultMaxDataSize = 1024 * 1024 * 4; // 4MB
    public const int DefaultMaxFragmentSize = 1024 * 4; // 4KB
    public const int DefaultMaxFragmentCount = 1000;
    public const int DefaultMemorySizeLimit = 1024 * 1024 * 500; // 500MB
    public const int DefaultMaxParentInMemory = 10_000;

    public static readonly BigCrystalOptions Default = new BigCrystalOptions();

    public BigCrystalOptions()
    {
    }

    internal BigCrystalOptions(string crystalDirectory)
    {
        this.CrystalDirectory = crystalDirectory;
    }

    /// <summary>
    /// Gets or sets a path of the directory where data files are stored.
    /// </summary>
    public string CrystalDirectory { get; set; } = DefaultCrystalDirectory;

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

    public string RootPath => this.rootPath ??= PathHelper.GetRootedDirectory(Directory.GetCurrentDirectory(), this.CrystalDirectory);

    public string CrystalFilePath => PathHelper.GetRootedFile(this.RootPath, this.CrystalFile);

    public string CrystalBackupPath => PathHelper.GetRootedFile(this.RootPath, this.CrystalBackup);

    public string StorageFilePath => PathHelper.GetRootedFile(this.RootPath, this.StorageFile);

    public string StorageBackupPath => PathHelper.GetRootedFile(this.RootPath, this.StorageBackup);

    internal void SetRootPath(string directory)
    {
        this.rootPath = PathHelper.GetRootedDirectory(directory, this.CrystalDirectory);
    }

    private string? rootPath;
}
