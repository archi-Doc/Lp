// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;

namespace Arc.Unit;

public class ConsoleLogger : ILogOutput
{
    public const string DefaultFormat = "";

    public ConsoleLogger()
    {
        this.format = DefaultFormat;
    }

    public ConsoleLogger(string format)
    {
        this.format = format;
    }

    public void Output(LogOutputParameter param)
    {
        var logLevelColors = this.GetLogLevelConsoleColors(param.LogLevel);
        string logLevelString = GetLogLevelString(param.LogLevel);

        this.textWriter.Write('[');

        string? timestamp = null;
        string? timestampFormat = "HH:mm:ss.fff";
        if (timestampFormat != null)
        {
            var dateTimeOffset = DateTimeOffset.Now;
            timestamp = dateTimeOffset.ToString(timestampFormat);
            this.textWriter.Write(timestamp);
        }

        this.textWriter.Write(' ');
        WriteColoredMessage(this.textWriter, logLevelString, logLevelColors.Background, logLevelColors.Foreground);
        if (param.EventId == 0)
        {
            this.textWriter.Write($" {param.LogSourceType.Name}] ");
        }
        else
        {
            this.textWriter.Write($" {param.LogSourceType.Name}({param.EventId.ToString()})] ");
        }

        WriteColoredMessage(this.textWriter, param.Message, null, ConsoleColor.White);

        // this.textWriter.Write(Environment.NewLine);

        var sb = this.textWriter.GetStringBuilder();
        Console.WriteLine(sb.ToString());
        sb.Clear();
    }

    private static void WriteColoredMessage(TextWriter textWriter, string message, ConsoleColor? background, ConsoleColor? foreground)
    {
        // Order: backgroundcolor, foregroundcolor, Message, reset foregroundcolor, reset backgroundcolor
        if (background.HasValue)
        {
            textWriter.Write(AnsiParser.GetBackgroundColorEscapeCode(background.Value));
        }

        if (foreground.HasValue)
        {
            textWriter.Write(AnsiParser.GetForegroundColorEscapeCode(foreground.Value));
        }

        textWriter.Write(message);
        if (foreground.HasValue)
        {
            textWriter.Write(AnsiParser.DefaultForegroundColor); // reset to default foreground color
        }

        if (background.HasValue)
        {
            textWriter.Write(AnsiParser.DefaultBackgroundColor); // reset to the background color
        }
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Fatal => "FTL",
            _ => string.Empty,
        };
    }

    private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Debug => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
            LogLevel.Information => new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black),
            LogLevel.Warning => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
            LogLevel.Error => new ConsoleColors(ConsoleColor.Black, ConsoleColor.DarkRed),
            LogLevel.Fatal => new ConsoleColors(ConsoleColor.White, ConsoleColor.DarkRed),
            _ => new ConsoleColors(null, null),
        };
    }

    private StringWriter textWriter = new();
    private string format;

    private readonly struct ConsoleColors
    {
        public ConsoleColors(ConsoleColor? foreground, ConsoleColor? background)
        {
            this.Foreground = foreground;
            this.Background = background;
        }

        public ConsoleColor? Foreground { get; }

        public ConsoleColor? Background { get; }
    }
}
