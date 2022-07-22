// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public record class ConsoleLoggerOptions
{
    public const int DefaultMaxQueue = 1000;

    public ConsoleLoggerOptions()
    {
        this.Formatter = new(true);
    }

    /// <summary>
    /// Gets <see cref="SimpleLogFormatterOptions"/>.
    /// </summary>
    public SimpleLogFormatterOptions Formatter { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of queued log (0 for unlimited).
    /// </summary>
    public int MaxQueue { get; set; } = DefaultMaxQueue;
}
