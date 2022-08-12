// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Arc.Threading;

namespace Arc.Unit;

public class ConsoleLogger : BufferedLogOutput
{
#pragma warning disable SA1310 // Field names should not contain underscore
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
#pragma warning restore SA1310 // Field names should not contain underscore

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    private static void SetConsoleMode()
    {
        try
        {
            var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            if (GetConsoleMode(iStdOut, out uint outConsoleMode))
            {
                outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
                SetConsoleMode(iStdOut, outConsoleMode);
            }
        }
        catch
        {
        }
    }

    public ConsoleLogger(UnitCore core, UnitLogger unitLogger, ConsoleLoggerOptions options)
        : base(unitLogger)
    {
        // Console
        SetConsoleMode();

        this.Formatter = new(options.Formatter);
        if (options.EnableBuffering)
        {
            this.worker = new(core, this);
        }

        this.options = options;
    }

    public override void Output(LogOutputParameter param)
    {
        if (this.worker == null)
        {
            Console.WriteLine(this.Formatter.Format(param));
            return;
        }

        if (this.options.MaxQueue <= 0 || this.worker.Count < this.options.MaxQueue)
        {
            this.worker.Add(new(param));
        }
    }

    public override Task<int> Flush(bool terminate) => this.worker?.Flush(terminate) ?? Task.FromResult(0);

    internal SimpleLogFormatter Formatter { get; init; }

    private ConsoleLoggerWorker? worker;
    private ConsoleLoggerOptions options;
}
