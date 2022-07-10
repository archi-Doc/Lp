// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Services;

public class ConsoleUserInterfaceService : IUserInterfaceService
{
    public async Task Notify(UserInterfaceNotifyLevel level, string message)
    {
        switch (level)
        {
            case UserInterfaceNotifyLevel.Debug:
                Logger.Default.Debug(message);
                break;

            case UserInterfaceNotifyLevel.Information:
                Logger.Default.Information(message);
                break;

            case UserInterfaceNotifyLevel.Warning:
                Logger.Default.Warning(message);
                break;

            case UserInterfaceNotifyLevel.Error:
                Logger.Default.Error(message);
                break;

            case UserInterfaceNotifyLevel.Fatal:
                Logger.Default.Fatal(message);
                break;

            default:
                break;
        }
    }

    public async Task<string?> RequestPassword(string? description)
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

                /*else if (key == ConsoleKey.Escape)
                {
                    Console.WriteLine();
                    return null;
                }*/
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

    public async Task<string?> RequestString(string? description)
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

    public async Task<bool?> RequestYesOrNo(string? description)
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
}
