// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;

namespace Arc.Unit;

public class SimpleLogFormatter
{
    internal const string DefaultForegroundColor = "\x1B[39m\x1B[22m"; // reset to default foreground color
    internal const string DefaultBackgroundColor = "\x1B[49m"; // reset to the background color
    internal const ConsoleColor DefaultColor = (ConsoleColor)(-1);

    public SimpleLogFormatter(SimpleLogFormatterOptions options)
    {
        this.options = options;
    }

    public string Format(LogOutputParameter param)
    {// [Timestamp Source(EventId) Level] Message (Exception)
        var sb = new StringBuilder();
        var logLevelColors = this.GetLogLevelConsoleColors(param.LogLevel);
        var logLevelString = this.GetLogLevelString(param.LogLevel);

        sb.Append('[');

        // Timestamp
        var timestampFormat = this.options.TimestampFormat;
        if (timestampFormat != null)
        {
            var dateTimeOffset = DateTimeOffset.Now;
            sb.Append(dateTimeOffset.ToString(timestampFormat));
            sb.Append(' ');
        }

        // Source(EventId)
        string source = param.LogSourceType == typeof(DefaultLog) ? "Default" : param.LogSourceType.Name;
        /*if (param.EventId == 0)
        {
            sb.Append($"{source} ");
        }
        else*/
        {
            sb.Append($"{source}({param.EventId.ToString()}) ");
        }

        // Level
        this.WriteColoredMessage(sb, logLevelString, logLevelColors.Background, logLevelColors.Foreground);
        sb.Append("] ");

        // Message
        this.WriteColoredMessage(sb, param.Message, DefaultColor, ConsoleColor.White);

        // sb.Append(Environment.NewLine);

        return sb.ToString();
    }

    private void WriteColoredMessage(StringBuilder sb, string message, ConsoleColor background, ConsoleColor foreground)
    {
        if (!this.options.EnableColor)
        {
            sb.Append(message);
            return;
        }

        if (background != DefaultColor)
        {
            sb.Append(AnsiParser.GetBackgroundColorEscapeCode(background));
        }

        if (foreground != DefaultColor)
        {
            sb.Append(AnsiParser.GetForegroundColorEscapeCode(foreground));
        }

        sb.Append(message);

        if (foreground != DefaultColor)
        {
            sb.Append(AnsiParser.DefaultForegroundColor); // reset to default foreground color
        }

        if (background != DefaultColor)
        {
            sb.Append(AnsiParser.DefaultBackgroundColor); // reset to the background color
        }
    }

    private string GetLogLevelString(LogLevel logLevel)
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
            _ => new ConsoleColors(DefaultColor, DefaultColor),
        };
    }

    private SimpleLogFormatterOptions options;

    private readonly struct ConsoleColors
    {
        public ConsoleColors(ConsoleColor foreground, ConsoleColor background)
        {
            this.Foreground = foreground;
            this.Background = background;
        }

        public ConsoleColor Foreground { get; }

        public ConsoleColor Background { get; }
    }

    internal static string GetForegroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\x1B[30m",
            ConsoleColor.DarkRed => "\x1B[31m",
            ConsoleColor.DarkGreen => "\x1B[32m",
            ConsoleColor.DarkYellow => "\x1B[33m",
            ConsoleColor.DarkBlue => "\x1B[34m",
            ConsoleColor.DarkMagenta => "\x1B[35m",
            ConsoleColor.DarkCyan => "\x1B[36m",
            ConsoleColor.Gray => "\x1B[37m",
            ConsoleColor.Red => "\x1B[1m\x1B[31m",
            ConsoleColor.Green => "\x1B[1m\x1B[32m",
            ConsoleColor.Yellow => "\x1B[1m\x1B[33m",
            ConsoleColor.Blue => "\x1B[1m\x1B[34m",
            ConsoleColor.Magenta => "\x1B[1m\x1B[35m",
            ConsoleColor.Cyan => "\x1B[1m\x1B[36m",
            ConsoleColor.White => "\x1B[1m\x1B[37m",
            _ => DefaultForegroundColor,
        };
    }

    internal static string GetBackgroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\x1B[40m",
            ConsoleColor.DarkRed => "\x1B[41m",
            ConsoleColor.DarkGreen => "\x1B[42m",
            ConsoleColor.DarkYellow => "\x1B[43m",
            ConsoleColor.DarkBlue => "\x1B[44m",
            ConsoleColor.DarkMagenta => "\x1B[45m",
            ConsoleColor.DarkCyan => "\x1B[46m",
            ConsoleColor.Gray => "\x1B[47m",
            _ => DefaultBackgroundColor,
        };
    }

    private static bool TryGetForegroundColor(int number, bool isBright, out ConsoleColor? color)
    {
        color = number switch
        {
            30 => ConsoleColor.Black,
            31 => isBright ? ConsoleColor.Red : ConsoleColor.DarkRed,
            32 => isBright ? ConsoleColor.Green : ConsoleColor.DarkGreen,
            33 => isBright ? ConsoleColor.Yellow : ConsoleColor.DarkYellow,
            34 => isBright ? ConsoleColor.Blue : ConsoleColor.DarkBlue,
            35 => isBright ? ConsoleColor.Magenta : ConsoleColor.DarkMagenta,
            36 => isBright ? ConsoleColor.Cyan : ConsoleColor.DarkCyan,
            37 => isBright ? ConsoleColor.White : ConsoleColor.Gray,
            _ => null,
        };

        return color != null || number == 39;
    }

    private static bool TryGetBackgroundColor(int number, out ConsoleColor? color)
    {
        color = number switch
        {
            40 => ConsoleColor.Black,
            41 => ConsoleColor.DarkRed,
            42 => ConsoleColor.DarkGreen,
            43 => ConsoleColor.DarkYellow,
            44 => ConsoleColor.DarkBlue,
            45 => ConsoleColor.DarkMagenta,
            46 => ConsoleColor.DarkCyan,
            47 => ConsoleColor.Gray,
            _ => null,
        };

        return color != null || number == 49;
    }
}
