// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;

namespace Arc.Unit;

public class SerilogLogger : ILogDestination
{
    public SerilogLogger(Serilog.ILogger logger)
    {
        this.logger = logger;
    }

    public void Debug(string message) => this.logger.Debug(message);

    public void Information(string message) => this.logger.Information(message);

    public void Warning(string message) => this.logger.Warning(message);

    public void Error(string message) => this.logger.Error(message);

    public void Fatal(string message) => this.logger.Fatal(message);

    private Serilog.ILogger logger;
}
