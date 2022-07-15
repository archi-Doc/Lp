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
    public class DefaultLogger : ISimpleLogger
    {
        public DefaultLogger()
        {
        }

        public void Debug(string message)
        {
            File?.Debug(message);
            if (viewMode)
            {
                Console?.Debug(message);
            }
        }

        public void Information(string message)
        {
            File?.Information(message);
            if (viewMode)
            {
                Console?.Information(message);
            }
        }

        public void Warning(string message)
        {
            File?.Warning(message);
            if (viewMode)
            {
                Console?.Warning(message);
            }
        }

        public void Error(string message)
        {
            File?.Error(message);
            if (viewMode)
            {
                Console?.Error(message);
            }
        }

        public void Fatal(string message)
        {
            this.FatalFlag = true;
            File?.Fatal(message);
            if (viewMode)
            {
                Console?.Fatal(message);
            }
        }

        public bool FatalFlag { get; private set; }
    }

    public class PriorityLogger : ISimpleLogger
    {
        public PriorityLogger()
        {
        }

        public void Debug(string message)
        {
            File?.Debug(message);
            Console?.Debug(message);
        }

        public void Information(string message)
        {
            File?.Information(message);
            Console?.Information(message);
        }

        public void Warning(string message)
        {
            File?.Warning(message);
            Console?.Warning(message);
        }

        public void Error(string message)
        {
            File?.Error(message);
            Console?.Error(message);
        }

        public void Fatal(string message)
        {
            File?.Fatal(message);
            Console?.Fatal(message);
        }

        public bool FatalFlag { get; private set; }
    }

    static Logger()
    {
        Default = new PriorityLogger(); // new DefaultLogger();
        Console = new EmptyLogger();
        File = new EmptyLogger();
        Background = new DefaultLogger();
    }

    public static void Configure(LPBase? info)
    {
        // Logger: Debug, Information, Warning, Error, Fatal
        Console = new SerilogLogger(new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger());

        if (info != null)
        {
            File = new SerilogLogger(new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(info.RootDirectory, "Logs", "log.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31,
                buffered: true,
                flushToDiskInterval: TimeSpan.FromMilliseconds(1000))
            .CreateLogger());
        }
    }

    public static bool ViewMode
    {
        get => Volatile.Read(ref viewMode);
        set => Volatile.Write(ref viewMode, value);
    }

    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }

    /// <summary>
    /// Gets a logging instance which output logs to <b>console and file</b>.
    /// </summary>
    public static ISimpleLogger Default { get; }

    /// <summary>
    /// Gets a logging instance which output logs to <b>file</b>.
    /// </summary>
    public static ISimpleLogger File { get; private set; }

    /// <summary>
    /// Gets a logging instance which output logs to <b>console</b>.
    /// </summary>
    public static ISimpleLogger Console { get; private set; }

    /// <summary>
    /// Gets a logging instance which output logs to <b>console (excepts in subcommand console) and file</b>.
    /// </summary>
    public static ISimpleLogger Background { get; }

    private static bool viewMode = true;
}
