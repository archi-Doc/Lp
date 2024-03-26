// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Logging;

internal class BackgroundAndFileLogger : ILogOutput
{
    public BackgroundAndFileLogger(IUserInterfaceService userInterfaceService, ConsoleLogger consoleLogger, FileLogger<FileLoggerOptions> fileLogger)
    {
        this.userInterfaceService = userInterfaceService;
        this.consoleLogger = consoleLogger;
        this.fileLogger = fileLogger;
    }

    public void Output(LogEvent logEvent)
    {// Fatai or Error or ViewMode -> Console and File, Others -> File
        if (logEvent.LogLevel == LogLevel.Fatal ||
            logEvent.LogLevel == LogLevel.Error ||
            this.userInterfaceService.IsViewMode)
        {
            this.consoleLogger.Output(logEvent);
        }

        this.fileLogger.Output(logEvent);
    }

    private IUserInterfaceService userInterfaceService;
    private ConsoleLogger consoleLogger;
    private FileLogger<FileLoggerOptions> fileLogger;
}
