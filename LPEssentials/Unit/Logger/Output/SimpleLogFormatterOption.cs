// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public record class SimpleLogFormatterOptions
{
    public SimpleLogFormatterOptions(bool enableColor)
    {
        this.EnableColor = enableColor;
    }

    public bool EnableColor { get; set; }

    /// <summary>
    /// Gets or sets the timestamp format (default is "HH:mm:ss.fff").
    /// </summary>
    public string? TimestampFormat { get; set; } = "HH:mm:ss.fff";
}
