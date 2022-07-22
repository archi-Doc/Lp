// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public static class ILoggerExtentions
{
    public static void Log(this ILogger logger, string message)
        => logger.Log(0, message);

    public static void Log(this ILogger logger, string message, Exception? exception)
        => logger.Log(0, message, exception);
}
