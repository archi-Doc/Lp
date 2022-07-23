// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Logging;

internal class BackgroundAndFileLogger : ILogOutput
{
    public BackgroundAndFileLogger(LPBase lpBase, ConsoleLogger consoleLogger, FileLogger<FileLoggerOptions> fileLogger)
    {
        this.lpBase = lpBase;
        this.consoleLogger = consoleLogger;
        this.fileLogger = fileLogger;
    }

    public void Output(LogOutputParameter param)
    {
        if (!this.lpBase.ConsoleMode)
        {
            this.consoleLogger.Output(param);
        }

        this.fileLogger.Output(param);
    }

    private LPBase lpBase;
    private ConsoleLogger consoleLogger;
    private FileLogger<FileLoggerOptions> fileLogger;
}
