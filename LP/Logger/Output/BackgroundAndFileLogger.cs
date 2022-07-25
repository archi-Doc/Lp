// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Services;

namespace LP.Logging;

internal class BackgroundAndFileLogger : ILogOutput
{
    public BackgroundAndFileLogger(IUserInterfaceService userInterfaceService, ConsoleLogger consoleLogger, FileLogger<FileLoggerOptions> fileLogger)
    {
        this.userInterfaceService = userInterfaceService;
        this.consoleLogger = consoleLogger;
        this.fileLogger = fileLogger;
    }

    public void Output(LogOutputParameter param)
    {// Fatai or Error or ViewMode -> Console and File, Others -> File
        if (param.LogLevel == LogLevel.Fatal ||
            param.LogLevel == LogLevel.Error ||
            this.userInterfaceService.IsViewMode)
        {
            this.consoleLogger.Output(param);
        }

        this.fileLogger.Output(param);
    }

    private IUserInterfaceService userInterfaceService;
    private ConsoleLogger consoleLogger;
    private FileLogger<FileLoggerOptions> fileLogger;
}
