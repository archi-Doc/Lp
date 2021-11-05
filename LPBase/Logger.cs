// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace LP;

public static class Logger
{
    public static void Configure(Information info)
    {
        // Logger: Debug, Information, Warning, Error, Fatal
        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.File(
            Path.Combine(info.RootDirectory, "logs", "log.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 31,
            buffered: true,
            flushToDiskInterval: TimeSpan.FromMilliseconds(1000))
        .CreateLogger();

        consoleLogger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console()
        .CreateLogger();
    }

    public static bool ViewMode
    {
        get => Volatile.Read(ref viewMode);
        set => Volatile.Write(ref viewMode, value);
    }

    public static void Debug(string message)
    {
        Log.Debug(message);
        if (viewMode)
        {
            consoleLogger?.Debug(message);
        }
    }

    public static void Information(string message)
    {
        Log.Information(message);
        if (viewMode)
        {
            consoleLogger?.Information(message);
        }
    }

    public static void Warning(string message)
    {
        Log.Warning(message);
        if (viewMode)
        {
            consoleLogger?.Warning(message);
        }
    }

    public static void Error(string message)
    {
        Log.Error(message);
        if (viewMode)
        {
            consoleLogger?.Error(message);
        }
    }

    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }

    private static Serilog.ILogger? consoleLogger;
    private static bool viewMode = true;
}
