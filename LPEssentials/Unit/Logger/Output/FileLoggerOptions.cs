// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public record class FileLoggerOptions
{
    public const string DefaultPath = "Log.txt";
    public const int DefaultMaxQueue = 1000;

    public FileLoggerOptions()
    {
        this.Formatter = new(false);
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

    internal bool PathFixed { get; set; } = false;
}
