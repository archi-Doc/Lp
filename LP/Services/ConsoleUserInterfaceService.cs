// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Services;

public class ConsoleUserInterfaceService : IUserInterfaceService
{
    public ConsoleUserInterfaceService(ILogger<DefaultLog> logger)
    {
        this.logger = logger;
    }

    public async Task Notify(LogLevel level, string message)
        => this.logger.TryGet(level)?.Log(message);

    public Task<string?> RequestPassword(string? description)
    {
        // return this.RequestPasswordInternal(description);
        return this.TaskRunAndWaitAsync(() => this.RequestPasswordInternal(description));

        /*try
        {
            return await Task.Run(() => this.RequestPasswordInternal(description)).WaitAsync(ThreadCore.Root.CancellationToken);
        }
        catch
        {
            Console.WriteLine();
            return null;
        }*/
    }

    public Task<string?> RequestString(string? description)
        => this.TaskRunAndWaitAsync(() => this.RequestStringInternal(description));

    public Task<bool?> RequestYesOrNo(string? description)
        => this.TaskRunAndWaitAsync(() => this.RequestYesOrNoInternal(description));

    private async Task<T?> TaskRunAndWaitAsync<T>(Func<Task<T>> func)
    {
        try
        {
            return await Task.Run(func).WaitAsync(ThreadCore.Root.CancellationToken);
        }
        catch
        {
            Console.WriteLine();
            return default;
        }
    }

    private async Task<string?> RequestPasswordInternal(string? description)
    {
        if (!string.IsNullOrEmpty(description))
        {
            Console.Write(description + ": ");
        }

        ConsoleKey key;
        var password = string.Empty;
        try
        {
            Console.TreatControlCAsInput = true;

            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;
                if (ThreadCore.Root.IsTerminated)
                {
                    return null;
                }

                if (key == ConsoleKey.Backspace && password.Length > 0)
                {
                    Console.Write("\b \b");
                    password = password[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    password += keyInfo.KeyChar;
                }
                else if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0 &&
                    (keyInfo.Key & ConsoleKey.C) != 0)
                {// Ctrl+C
                    Console.WriteLine();
                    return null;
                }
                else if (key == ConsoleKey.Escape)
                {
                    Console.WriteLine();
                    return null;
                }
            }
            while (key != ConsoleKey.Enter);
        }
        finally
        {
            Console.TreatControlCAsInput = false;
        }

        Console.WriteLine();
        return password;
    }

    private async Task<string?> RequestStringInternal(string? description)
    {
        if (!string.IsNullOrEmpty(description))
        {
            Console.Write(description + ": ");
        }

        while (true)
        {
            var input = Console.ReadLine();
            if (input == null)
            {// Ctrl+C
                Console.WriteLine();
                return null; // throw new PanicException();
            }

            input = input.CleanupInput();
            if (input == string.Empty)
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
            Console.WriteLine(description + " [Y/n]");
        }

        while (true)
        {
            var input = Console.ReadLine();
            if (input == null)
            {// Ctrl+C
                Console.WriteLine();
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
                Console.WriteLine("[Y/n]");
            }
        }
    }

    private ILogger<DefaultLog> logger;
}
