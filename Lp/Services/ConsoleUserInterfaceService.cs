// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimplePrompt;

namespace Lp.Services;

internal class ConsoleUserInterfaceService : IUserInterfaceService
{
    private const string YesOrNoSuffix = "[Y/n] ";

    private readonly ILogger logger;
    private readonly SimpleConsole simpleConsole;

    private readonly ReadLineOptions passwordOptions = new()
    {
        AllowEmptyLineInput = true,
        MaxInputLength = 100,
        MaskingCharacter = '*',
        MultilineDelimiter = default,
    };

    public ConsoleUserInterfaceService(ILogger<DefaultLog> logger, SimpleConsole simpleConsole)
    {
        this.logger = logger;
        this.simpleConsole = simpleConsole;
    }

    public override void Write(string? message = null)
        => this.simpleConsole.Write(message);

    public override void WriteLine(string? message = null)
        => this.simpleConsole.WriteLine(message);

    public override void EnqueueLine(string? message = null)
        => this.simpleConsole.EnqueueInput(message);

    public override Task<InputResult> ReadLine(CancellationToken cancellationToken)
        => this.simpleConsole.ReadLine(default, cancellationToken);

    public override ConsoleKeyInfo ReadKey(bool intercept)
        => ((IConsoleService)this.simpleConsole).ReadKey(intercept);

    public override bool KeyAvailable
        => ((IConsoleService)this.simpleConsole).KeyAvailable;

    public override async Task Notify(LogLevel level, string message)
        => this.logger.TryGet(level)?.Log(message);

    public override async Task<string?> ReadPassword(string? description)
    {
        var options = this.passwordOptions with
        {
            Prompt = description ?? string.Empty,
        };

        var result = await this.simpleConsole.ReadLine(options).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            return result.Text;
        }
        else
        {
            return null;
        }
    }

    public override Task<InputResult> ReadLine(bool cancelOnEscape, string? description)
    {
        var options = new ReadLineOptions
        {
            CancelOnEscape = cancelOnEscape,
            MultilineDelimiter = default,
            Prompt = description ?? string.Empty,
        };

        return this.simpleConsole.ReadLine(options);
    }

    public override async Task<bool?> ReadYesNo(string? description)
    {
        var options = ReadLineOptions.YesNo with
        {
            Prompt = description == null ? YesOrNoSuffix : $"{description} {YesOrNoSuffix}",
            CancelOnEscape = true,
        };

        while (true)
        {
            var result = await this.simpleConsole.ReadLine(options).ConfigureAwait(false);
            if (result.Kind == InputResultKind.Terminated ||
                result.Kind == InputResultKind.Canceled)
            {// Ctrl+C
                // this.WriteLine();
                return null; // throw new PanicException();
            }

            var text = result.Text.CleanupInput().ToLower();
            if (text == "y" || text == "yes")
            {
                return true;
            }
            else if (text == "n" || text == "no")
            {
                return false;
            }
            else
            {
                return null;
            }
        }
    }

    /*private async Task<string?> RequestPasswordInternal(string? description)
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
    }*/
}
