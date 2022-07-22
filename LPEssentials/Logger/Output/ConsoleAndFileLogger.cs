// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Logging;

internal class ConsoleAndFileLogger : ILogOutput
{
    public ConsoleAndFileLogger(ConsoleLogger consoleLogger, FileLogger<FileLoggerOptions> fileLogger)
    {
        this.consoleLogger = consoleLogger;
        this.fileLogger = fileLogger;
    }

    public void Output(LogOutputParameter param)
    {
        this.consoleLogger.Output(param);
        this.fileLogger.Output(param);
    }

    private ConsoleLogger consoleLogger;
    private FileLogger<FileLoggerOptions> fileLogger;
}
