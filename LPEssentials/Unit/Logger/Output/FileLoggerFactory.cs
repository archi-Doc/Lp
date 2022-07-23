// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace Arc.Unit;

internal class FileLoggerFactory<TOption> : FileLogger<TOption>
    where TOption : FileLoggerOptions
{
    public FileLoggerFactory(UnitCore core, UnitLogger unitLogger, TOption options)
        : base(core, unitLogger, options)
    {
    }
}
