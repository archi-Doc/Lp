// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public class FileLoggerOptions
{
    public const string DefaultPath = "Log.txt";
    public const int DefaultMaxQueue = 10_000;

    public FileLoggerOptions()
    {
        this.Formatter = new(false);
        this.Formatter.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff K";
    }

    public string Path { get; set; } = DefaultPath;

    /// <summary>
    /// Gets <see cref="SimpleLogFormatterOptions"/>.
    /// </summary>
    public SimpleLogFormatterOptions Formatter { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of queued log (0 for unlimited).
    /// </summary>
    public int MaxQueue { get; set; } = DefaultMaxQueue;

    /// <summary>
    /// Gets or sets the upper limit of log capacity in megabytes.
    /// </summary>
    public int MaxLogCapacity { get; set; } = 100;
}
