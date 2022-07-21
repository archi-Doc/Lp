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
        switch (logLevel)
        {
            case LogLevel.Debug:
                this.logger.Debug(message);
                break;

            case LogLevel.Information:
                this.logger.Information(message);
                break;

            case LogLevel.Warning:
                this.logger.Warning(message);
                break;

            case LogLevel.Error:
                this.logger.Error(message);
                break;

            case LogLevel.Fatal:
                this.logger.Fatal(message);
                break;
        }
    }

    private Serilog.ILogger logger;
}
