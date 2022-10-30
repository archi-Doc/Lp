// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Logging;

public class ClientTerminalLoggerOptions : StreamLoggerOptions
{
}

public class ServerTerminalLoggerOptions : StreamLoggerOptions
{
}

public class StreamLoggerOptions : FileLoggerOptions
{
}

internal class StreamLogger<TOption> : BufferedLogOutput
    where TOption : StreamLoggerOptions
{
    public StreamLogger(UnitCore core, UnitLogger unitLogger, TOption options)
        : base(unitLogger)
    {
    }

    public override async Task<int> Flush(bool terminate)
    {
        return 0;
    }

    public override void Output(LogOutputParameter param)
    {
        // this.fileLogger.Output(param);
    }
}
