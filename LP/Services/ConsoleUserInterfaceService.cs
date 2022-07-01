// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Services;

public class ConsoleUserInterfaceService : IUserInterfaceService
{
    public async Task<string> RequestString(string? description)
    {
        if (!string.IsNullOrEmpty(description))
        {
            Console.Write(description + ": ");
        }

        while (true)
        {
            var input = Console.ReadLine()?.ToLower();
            if (input == null)
            {// Ctrl+C
                Console.WriteLine();
                throw new PanicException();
            }

            input = input.Ceanup();
            if (input == string.Empty)
            {
                continue;
            }

            return input;
        }
    }

    public async Task<bool> RequestYesOrNo(string? description)
    {
        if (!string.IsNullOrEmpty(description))
        {
            Console.WriteLine(description + " [Y/n]");
        }

        while (true)
        {
            var input = Console.ReadLine()?.ToLower();
            if (input == null)
            {// Ctrl+C
                Console.WriteLine();
                throw new PanicException();
            }
            else if (input == "y" || input == "yes")
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
