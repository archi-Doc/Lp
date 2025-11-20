// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimplePrompt;

namespace Lp.Services;

internal class ConsoleUserInterfaceService : IUserInterfaceService
{
    private readonly UnitCore core;
    private readonly ILogger logger;
    private readonly SimpleConsole simpleConsole;
    private readonly IConsoleService consoleService;

    private readonly SimpleConsoleOptions passwordOptions = new()
    {

    };

    public ConsoleUserInterfaceService(UnitCore core, ILogger<DefaultLog> logger, SimpleConsole simpleConsole)
    {
        this.core = core;
        this.logger = logger;
        this.simpleConsole = simpleConsole;
        this.consoleService = simpleConsole;
    }

    public override void Write(string? message = null)
        => this.consoleService.Write(message);

    public override void WriteLine(string? message = null)
        => this.consoleService.WriteLine(message);

    public override void EnqueueInput(string? message = null)
    {
        //this.consoleTextReader.Enqueue(message);
    }

    public override Task<InputResult> ReadLine(CancellationToken cancellationToken)
        => this.simpleConsole.ReadLine(default, cancellationToken);

    public override ConsoleKeyInfo ReadKey(bool intercept)
        => this.consoleService.ReadKey(intercept);

    public override bool KeyAvailable
        => this.consoleService.KeyAvailable;

    public override async Task Notify(LogLevel level, string message)
        => this.logger.TryGet(level)?.Log(message);

    public override async Task<string?> RequestPassword(string? description)
    {
        //this.simpleConsole.UnderlyingTextWriter.Write(description);
        var options = this.passwordOptions with
        {
            Prompt = description ?? string.Empty,
        };

        var result = await this.simpleConsole.ReadLine(options).ConfigureAwait(false);
        return result.Text;
    }

    public override Task<string?> RequestString(bool enterToExit, string? description)
        => this.RequestStringInternal(enterToExit, description);

    public override Task<bool?> RequestYesOrNo(string? description)
        => this.RequestYesOrNoInternal(description);

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
