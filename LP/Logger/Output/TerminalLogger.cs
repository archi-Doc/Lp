// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Logging;

public class TerminalLoggerOptions : FileLoggerOptions
{
}

internal class TerminalLogger : ILogOutput
{
    public TerminalLogger(ConsoleLogger consoleLogger, FileLogger<TerminalLoggerOptions> fileLogger)
    {
        this.consoleLogger = consoleLogger;
        this.fileLogger = fileLogger;
    }

    public void Output(LogOutputParameter param)
    {
        this.fileLogger.Output(param);
    }

    private ConsoleLogger consoleLogger;
    private FileLogger<TerminalLoggerOptions> fileLogger;
}
