// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Logging;

internal class BackgroundAndFileLogger : ILogOutput
{
    public BackgroundAndFileLogger(Control control, ConsoleLogger consoleLogger, FileLogger<FileLoggerOptions> fileLogger)
    {
        this.control = control;
        this.consoleLogger = consoleLogger;
        this.fileLogger = fileLogger;
    }

    public void Output(LogOutputParameter param)
    {
        if (!this.control.ConsoleMode)
        {
            this.consoleLogger.Output(param);
        }

        this.fileLogger.Output(param);
    }

    private Control control;
    private ConsoleLogger consoleLogger;
    private FileLogger<FileLoggerOptions> fileLogger;
}
