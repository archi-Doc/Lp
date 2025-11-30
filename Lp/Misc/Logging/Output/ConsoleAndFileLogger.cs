// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Logging;

internal class ConsoleAndFileLogger : ILogOutput
{
    public ConsoleAndFileLogger(IUserInterfaceService userInterfaceService, ConsoleLogger consoleLogger, FileLogger<FileLoggerOptions> fileLogger)
    {
        this.userInterfaceService = userInterfaceService;
        this.consoleLogger = consoleLogger;
        this.fileLogger = fileLogger;
    }

    public void Output(LogEvent logEvent)
    {
        if (logEvent.LogLevel >= LogLevel.Information)
        {// Fatal, Error, Warning, Information
            this.consoleLogger.Output(logEvent);
        }

        this.fileLogger.Output(logEvent);
    }

    private IUserInterfaceService userInterfaceService;
    private ConsoleLogger consoleLogger;
    private FileLogger<FileLoggerOptions> fileLogger;
}
