// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;

namespace Arc.Unit;

public class SerilogLogger : ILogOutput
{
    public SerilogLogger(Serilog.ILogger logger)
    {
        this.logger = logger;
    }

    public void Output(LogOutputParameter param)
    {
        switch (param.LogLevel)
        {
            case LogLevel.Debug:
                this.logger.Debug(param.Message);
                break;

            case LogLevel.Information:
                this.logger.Information(param.Message);
                break;

            case LogLevel.Warning:
                this.logger.Warning(param.Message);
                break;

            case LogLevel.Error:
                this.logger.Error(param.Message);
                break;

            case LogLevel.Fatal:
                this.logger.Fatal(param.Message);
                break;
        }
    }

    private Serilog.ILogger logger;
}
