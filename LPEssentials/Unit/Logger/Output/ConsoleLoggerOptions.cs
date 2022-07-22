// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public record class ConsoleLoggerOptions
{
    public ConsoleLoggerOptions()
    {
        this.Formatter = new(true);
    }

    public SimpleLogFormatterOptions Formatter { get; init; }
}
