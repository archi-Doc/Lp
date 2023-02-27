// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Logging;

internal class ConsoleAndFileLogger : ILogOutput
{
    public ConsoleAndFileLogger(IUserInterfaceService userInterfaceService, ConsoleLogger consoleLogger, FileLogger<FileLoggerOptions> fileLogger)
    {
        this.userInterfaceService = userInterfaceService;
        this.consoleLogger = consoleLogger;
        this.fileLogger = fileLogger;
    }

    public void Output(LogOutputParameter param)
    {// Fatai or Error or !InputMode -> Console and File, Others -> File
        if (param.LogLevel == LogLevel.Fatal ||
            param.LogLevel == LogLevel.Error ||
            !this.userInterfaceService.IsInputMode)
        {
            this.consoleLogger.Output(param);
        }

        this.fileLogger.Output(param);
    }

    private IUserInterfaceService userInterfaceService;
    private ConsoleLogger consoleLogger;
    private FileLogger<FileLoggerOptions> fileLogger;
}
