// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public record ZenOptions
{
    public const string DefaultZenDirectory = "Zen";

    public static ZenOptions Standard { get; } = new ZenOptions();

    /// <summary>
    /// Initializes a new instance of the <see cref="ZenOptions"/> class.
    /// </summary>
    protected internal ZenOptions()
    {
        this.ZenPath = Directory.GetCurrentDirectory();
        this.SnowflakePath = System.IO.Path.Combine(this.ZenPath, DefaultZenDirectory);
    }

    /// <summary>
    /// Gets a path of the directory where <see cref="Zen"/> files are stored.
    /// </summary>
    public string ZenPath { get; init; }

    /// <summary>
    /// Gets a path of the default directory where <see cref="Snowflake"/> files are stored.
    /// </summary>
    public string SnowflakePath { get; init; }

    public int MaxDataSize { get; init; } = 1024 * 1024 * 4; // 4MB

    public int MaxFragmentSize { get; init; } = 1024 * 4; // 4KB

    public int MaxFragmentCount { get; init; } = 1000;

    public long MemorySizeLimit { get; init; } = 1024 * 1024 * 100; // 100MB

    public long DirectoryCapacity { get; init; } = 1024L * 1024 * 1024 * 10; // 10GB

    public string ZenFile { get; init; } = "Zen.main";

    public string ZenBackup { get; init; } = "Zen.back";

    public string ZenDirectoryFile { get; init; } = "ZenDirectory.main";

    public string ZenDirectoryBackup { get; init; } = "ZenDirectory.back";

    public string SnowflakeFile { get; init; } = "Snowflake.main";

    public string SnowflakeBackup { get; init; } = "Snowflake.back";
}
