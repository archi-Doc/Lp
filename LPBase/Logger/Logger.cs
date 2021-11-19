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
        Default = new DefaultLogger();
        Console = new EmptyLogger();
        File = new EmptyLogger();
        Priority = new PriorityLogger();
    }

    public static void Configure(Information info)
    {
        // Logger: Debug, Information, Warning, Error, Fatal
        File = new SerilogLogger(new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.File(
            Path.Combine(info.RootDirectory, "logs", "log.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 31,
            buffered: true,
            flushToDiskInterval: TimeSpan.FromMilliseconds(1000))
        .CreateLogger());

        Console = new SerilogLogger(new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.Console()
        .CreateLogger());
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

    public static ISimpleLogger Default { get; }

    public static ISimpleLogger File { get; private set; }

    public static ISimpleLogger Console { get; private set; }

    public static ISimpleLogger Priority { get; }

    private static bool viewMode = true;
}
