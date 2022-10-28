// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Services;

internal class ConsoleService : IConsoleService
{
    public void Write(string? message)
    {
        try
        {
            Console.Write(message);
        }
        catch
        {
        }
    }

    public void WriteLine(string? message)
    {
        try
        {
            Console.WriteLine(message);
        }
        catch
        {
        }
    }
}
