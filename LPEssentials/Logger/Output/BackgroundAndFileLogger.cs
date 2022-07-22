// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Logging;

internal class BackgroundAndFileLogger : ILogOutput
{
    public BackgroundAndFileLogger(ConsoleLogger consoleLogger, FileLogger<FileLoggerOptions> fileLogger)
    {
        this.consoleLogger = consoleLogger;
        this.fileLogger = fileLogger;
    }

    public void Output(LogOutputParameter param)
    {
        if (LP.Logger.ViewMode)
        {
            this.consoleLogger.Output(param);
        }

        this.fileLogger.Output(param);
    }

    private ConsoleLogger consoleLogger;
    private FileLogger<FileLoggerOptions> fileLogger;
}
