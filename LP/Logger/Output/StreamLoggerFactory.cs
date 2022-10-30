// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Logging;

internal class StreamLoggerFactory<TOption> : StreamLogger<TOption>
    where TOption : StreamLoggerOptions
{
    public StreamLoggerFactory(UnitCore core, UnitLogger unitLogger, TOption options)
        : base(core, unitLogger, options)
    {
    }
}
