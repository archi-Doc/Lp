// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public static class TestLog
{
    public static void Add(string message)
    {
        if (LogPath == null)
        {
            return;
        }

        try
        {
            File.AppendAllText(LogPath, $"{DateTime.Now.ToString()} {message}\r\n");
        }
        catch
        {
        }
    }

    public static string? LogPath { get; set; } = "C:\\App\\Test.log";
}
