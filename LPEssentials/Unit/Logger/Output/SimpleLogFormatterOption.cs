// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public record class SimpleLogFormatterOptions(
    bool EnableColor,
    string? TimestampFormat = "HH:mm:ss.fff");
