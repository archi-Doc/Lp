// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record CrystalOptions
{
    public const long DefaultDirectoryCapacity = 1024L * 1024 * 1024 * 10; // 10GB
    public const int DefaultMaxDataSize = 1024 * 1024 * 4; // 4MB
    public const int DefaultMaxFragmentSize = 1024 * 4; // 4KB
    public const int DefaultMaxFragmentCount = 1000;
    public const int DefaultMemorySizeLimit = 1024 * 1024 * 500; // 500MB

    public static CrystalOptions Default { get; } = new CrystalOptions();

    /// <summary>
    /// Initializes a new instance of the <see cref="CrystalOptions"/> class.
    /// </summary>
    public CrystalOptions()
    {
    }

    /// <summary>
    /// Gets a path of the directory where <see cref="Crystal{TData}"/> files are stored.
    /// </summary>
    public string CrystalPath { get; init; } = string.Empty;

    public int MaxDataSize { get; init; } = DefaultMaxDataSize;

    public int MaxFragmentSize { get; init; } = DefaultMaxFragmentSize;

    public int MaxFragmentCount { get; init; } = DefaultMaxFragmentCount;

    public long MemorySizeLimit { get; init; } = DefaultMemorySizeLimit;

    public long DirectoryCapacity { get; init; } = 1024L * 1024 * 1024 * 10; // 10GB

    public string CrystalFile { get; init; } = "Crystal.main";

    public string CrystalBackup { get; init; } = "Crystal.back";

    public string CrystalDirectoryFile { get; init; } = "CrystalDirectory.main";

    public string CrystalDirectoryBackup { get; init; } = "CrystalDirectory.back";

    public string DefaultCrystalDirectory { get; init; } = "Crystal";

    public string SnowflakeFile { get; init; } = "Snowflake.main";

    public string SnowflakeBackup { get; init; } = "Snowflake.back";

    public string RootPath => this.rootPath ??= PathHelper.GetRootedDirectory(Directory.GetCurrentDirectory(), this.CrystalPath);

    public string CrystalFilePath => PathHelper.GetRootedFile(this.RootPath, this.CrystalFile);

    public string CrystalBackupPath => PathHelper.GetRootedFile(this.RootPath, this.CrystalBackup);

    public string CrystalDirectoryFilePath => PathHelper.GetRootedFile(this.RootPath, this.CrystalDirectoryFile);

    public string CrystalDirectoryBackupPath => PathHelper.GetRootedFile(this.RootPath, this.CrystalDirectoryBackup);

    private string? rootPath;
}
