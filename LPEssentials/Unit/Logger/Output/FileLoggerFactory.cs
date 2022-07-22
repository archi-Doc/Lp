// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

internal class FileLoggerFactory<TOption> : FileLogger<TOption>
    where TOption : FileLoggerOptions
{
    public FileLoggerFactory(UnitLogger unitLogger, TOption options)
        : base(unitLogger, options)
    {
    }
}
