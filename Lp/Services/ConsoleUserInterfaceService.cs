// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Services;

internal class ConsoleUserInterfaceService : IUserInterfaceService
{
    public ConsoleUserInterfaceService(UnitCore core, ILogger<DefaultLog> logger)
    {
        this.core = core;
        this.logger = logger;
    }

    public override void Write(string? message = null)
    {
        try
        {
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
            Console.WriteLine(message);
        }
        catch
        {
        }
    }

    public override string? ReadLine()
    {
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
                this.WriteLine("Ctrl+C");
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

    private UnitCore core;
    private ILogger<DefaultLog> logger;
}
