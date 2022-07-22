// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand.Logging;

namespace Arc.Unit;

internal class FileLoggerFactory<TOption> : FileLogger<TOption>
    where TOption : FileLoggerOptions
{
    public FileLoggerFactory(UnitLogger unitLogger, TOption options)
        : base(unitLogger, options)
    {
        // typeof(FileLogger<>).MakeGenericType(new Type[] { typeof(TOption), });
        // this.logger = unitLogger.Get<TLogSource>();
    }
}
