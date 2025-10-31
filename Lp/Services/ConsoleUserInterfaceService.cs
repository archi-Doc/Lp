// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Text;

namespace Lp.Services;

public class ConsoleBuffer
{
    private const int BufferSize = 1_024;

    private readonly Lock lockObject = new();
    private readonly char[] buffer = new char[BufferSize];
    private int promptLength;
    private int textLength;

    public ConsoleBuffer()
    {
    }

    public void Flush(string? prompt = default)
    {
        string? text = default;
        using (this.lockObject.EnterScope())
        {
            if (this.textLength > 0)
            {
                text = new string(this.buffer, this.promptLength, this.textLength);
            }

            if (prompt?.Length > 0)
            {
                prompt.AsSpan(0, Math.Max(prompt.Length, BufferSize)).CopyTo(this.buffer);
                this.promptLength = prompt.Length;
                this.textLength = 0;
            }
        }

        /*if (text is not null)
        {
            Console.WriteLine(text);
        }*/

        if (prompt?.Length > 0)
        {
            Console.Write(prompt);
        }
    }

    public string? ReadLine(ReadOnlySpan<char> prompt = default)
    {
        try
        {
            ConsoleKeyInfo key;
            while ((key = Console.ReadKey(intercept: true)).Key != ConsoleKey.Enter)
            {
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (this.textLength > 0)
                    {
                        this.textLength--;
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    this.buffer[this.textLength++] = key.KeyChar;
                    Console.Write(key.KeyChar);
                }
            }

            var result = new string(this.buffer, 0, this.textLength);
            this.textLength = 0;
            Console.WriteLine();
            return result;
        }
        catch
        {
            return null;
        }
    }
}

internal class ConsoleUserInterfaceService : IUserInterfaceService
{
    private readonly UnitCore core;
    private readonly ILogger logger;
    private readonly ConsoleTextReader consoleTextReader;
    private readonly ConsoleBuffer consoleBuffer = new();

    private class ConsoleTextReader : TextReader
    {
        private readonly TextReader original;
        private readonly ConcurrentQueue<string?> queue = new();

        public ConsoleTextReader(TextReader original)
        {
            this.original = original;
        }

        public void Enqueue(string? line)
        {
            this.queue.Enqueue(line);
        }

        public override string? ReadLine()
        {
            StringBuilder? sb = default;
            int tripleQuotesCount = 0;

Loop:
            if (this.queue.TryDequeue(out var line))
            {
                if (ProcessLine(line))
                {
                    goto Loop;
                }

                if (sb is null)
                {
                    return line;
                }
                else
                {
                    sb.Append(line);
                    return sb.ToString();
                }
            }

            var st = this.original.ReadLine();
            if (ProcessLine(st))
            {
                goto Loop;
            }

            if (sb is null)
            {
                return st;
            }
            else
            {
                sb.Append(st);
                return sb.ToString();
            }

            bool ProcessLine(string? content)
            {
                if (content is not null)
                {
                    tripleQuotesCount += CheckTripleQuotes(content);
                    if (content.EndsWith('\\'))
                    {
                        sb ??= new();
                        sb.Append(content[0..^1]);
                        sb.Append(' ');
                        return true;
                    }
                    else if ((tripleQuotesCount & 1) != 0)
                    {
                        sb ??= new();
                        sb.Append(content);
                        sb.Append(Environment.NewLine);
                        return true;
                    }
                }

                return false;
            }

            static int CheckTripleQuotes(ReadOnlySpan<char> text)
            {
                int count = 0;
                int index = 0;
                var span = text;
                while ((index = span.IndexOf("\"\"\"", StringComparison.Ordinal)) != -1)
                {
                    count++;
                    span = span.Slice(index + 3);
                }

                return count;
            }
        }
    }

    public ConsoleUserInterfaceService(UnitCore core, ILogger<DefaultLog> logger)
    {
        this.core = core;
        this.logger = logger;
        this.consoleTextReader = new ConsoleTextReader(Console.In);

        Console.SetIn(this.consoleTextReader);
    }

    public override void Write(string? message = null)
    {
        try
        {
            if (Environment.NewLine == "\r\n" && message is not null)
            {
                message = Arc.BaseHelper.ConvertLfToCrLf(message);
            }

            Console.Write(message);
        }
        catch
        {
        }
    }

    public override void WriteLine(string? message = null)
    {
        try
        {
            if (Environment.NewLine == "\r\n" && message is not null)
            {
                message = Arc.BaseHelper.ConvertLfToCrLf(message);
            }

            // Console.WriteLine($"{this.CurrentMode} : {message}");
            //if (Console.CursorTop > 0 && Console.CursorLeft > 0)
            if (this.CurrentMode == Mode.Console && Console.CursorTop > 0 && Console.CursorLeft == 2)
            {
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.WriteLine(message);
                Console.Write(LpConstants.InputString);
            }
            else
            {
                Console.WriteLine(message);
            }
        }
        catch
        {
        }
    }

    public override void EnqueueInput(string? message = null)
    {
        this.consoleTextReader.Enqueue(message);
    }

    public override string? ReadLine()
    {
        // return this.consoleBuffer.ReadLine();

        try
        {
            return Console.ReadLine();
        }
        catch
        {
            return null;
        }
    }

    public override ConsoleKeyInfo ReadKey(bool intercept)
    {
        try
        {
            return Console.ReadKey(intercept);
        }
        catch
        {
            return default;
        }
    }

    public override bool KeyAvailable
    {
        get
        {
            try
            {
                return Console.KeyAvailable;
            }
            catch
            {
                return false;
            }
        }
    }

    public override async Task Notify(LogLevel level, string message)
        => this.logger.TryGet(level)?.Log(message);

    public override Task<string?> RequestPassword(string? description)
    {
        // return this.RequestPasswordInternal(description);
        return this.TaskRunAndWaitAsync(() => this.RequestPasswordInternal(description));

        /*try
        {
            return await Task.Run(() => this.RequestPasswordInternal(description)).WaitAsync(ThreadCore.Root.CancellationToken).ConfigureAwait(false);
        }
        catch
        {
            this.consoleService.WriteLine();
            return null;
        }*/
    }

    public override Task<string?> RequestString(bool enterToExit, string? description)
        => this.TaskRunAndWaitAsync(() => this.RequestStringInternal(enterToExit, description));

    public override Task<bool?> RequestYesOrNo(string? description)
        => this.TaskRunAndWaitAsync(() => this.RequestYesOrNoInternal(description));

    private static async Task<ConsoleKeyInfo> ReadKeyAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                if (Console.KeyAvailable)
                {
                    return Console.ReadKey(intercept: true);
                }

                await Task.Delay(1000, cancellationToken);
            }
            catch
            {
                return default;
            }
        }
    }

    private async Task<T?> TaskRunAndWaitAsync<T>(Func<Task<T>> func)
    {
        var previous = this.ChangeMode(Mode.Input);
        try
        {
            return await Task.Run(func).WaitAsync(this.core.CancellationToken).ConfigureAwait(false);
        }
        catch
        {
            this.WriteLine();
            return default;
        }
        finally
        {
            this.ChangeMode(previous);
        }
    }

    private async Task<string?> RequestPasswordInternal(string? description)
    {
        if (!string.IsNullOrEmpty(description))
        {
            this.Write(description + ": ");
        }

        ConsoleKey key;
        var password = string.Empty;
        try
        {
            Console.TreatControlCAsInput = true;

            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                if (keyInfo == default || ThreadCore.Root.IsTerminated)
                {
                    return null;
                }

                key = keyInfo.Key;
                if (key == ConsoleKey.Backspace && password.Length > 0)
                {
                    this.Write("\b \b");
                    password = password[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    this.Write("*");
                    password += keyInfo.KeyChar;
                }
                else if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0 &&
                    (keyInfo.Key & ConsoleKey.C) != 0)
                {// Ctrl+C
                    this.WriteLine();
                    return null;
                }
                else if (key == ConsoleKey.Escape)
                {
                    this.WriteLine();
                    return null;
                }
            }
            while (key != ConsoleKey.Enter);
        }
        finally
        {
            Console.TreatControlCAsInput = false;
        }

        this.WriteLine();
        return password;
    }

    private async Task<string?> RequestStringInternal(bool enterToExit, string? description)
    {
        if (!string.IsNullOrEmpty(description))
        {
            this.Write(description + ": ");
        }

        while (true)
        {
            var input = Console.ReadLine();
            if (input == null)
            {// Ctrl+C
                this.WriteLine();
                return null; // throw new PanicException();
            }

            input = input.CleanupInput();
            if (input == string.Empty && !enterToExit)
            {
                continue;
            }

            return input;
        }
    }

    private async Task<bool?> RequestYesOrNoInternal(string? description)
    {
        if (!string.IsNullOrEmpty(description))
        {
            this.WriteLine(description + " [Y/n]");
        }

        while (true)
        {
            var input = Console.ReadLine();
            if (input == null)
            {// Ctrl+C
                this.WriteLine();
                return null; // throw new PanicException();
            }

            input = input.CleanupInput().ToLower();
            if (input == "y" || input == "yes")
            {
                return true;
            }
            else if (input == "n" || input == "no")
            {
                return false;
            }
            else
            {
                this.WriteLine("[Y/n]");
            }
        }
    }
}
