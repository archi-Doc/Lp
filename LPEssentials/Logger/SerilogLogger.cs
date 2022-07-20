// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;

namespace Arc.Unit;

public class SerilogLogger : ILogOutput
{
    public SerilogLogger(Serilog.ILogger logger)
    {
        this.logger = logger;
    }

    public void Output(Type logSourceType, LogLevel logLevel, string message)
    {
        this.logger.Information(message);
    }

    private Serilog.ILogger logger;
}
