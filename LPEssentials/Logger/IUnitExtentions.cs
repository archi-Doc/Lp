// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;

namespace LP;

public static class IUnitExtentions
{
    public static void Debug(this IUnitLogger logger, ulong hash) => logger.Debug(HashedString.Get(hash));

    public static void Information(this IUnitLogger logger, ulong hash) => logger.Information(HashedString.Get(hash));

    public static void Warning(this IUnitLogger logger, ulong hash) => logger.Warning(HashedString.Get(hash));

    public static void Error(this IUnitLogger logger, ulong hash) => logger.Error(HashedString.Get(hash));

    public static void Fatal(this IUnitLogger logger, ulong hash) => logger.Fatal(HashedString.Get(hash));

    public static void Debug(this IUnitLogger logger, ulong hash, object obj1)
        => logger.Debug(HashedString.Get(hash, obj1));

    public static void Information(this IUnitLogger logger, ulong hash, object obj1)
        => logger.Information(HashedString.Get(hash, obj1));

    public static void Warning(this IUnitLogger logger, ulong hash, object obj1)
        => logger.Warning(HashedString.Get(hash, obj1));

    public static void Error(this IUnitLogger logger, ulong hash, object obj1)
        => logger.Error(HashedString.Get(hash, obj1));

    public static void Fatal(this IUnitLogger logger, ulong hash, object obj1)
        => logger.Fatal(HashedString.Get(hash, obj1));

    public static void Debug(this IUnitLogger logger, ulong hash, object obj1, object obj2)
        => logger.Debug(HashedString.Get(hash, obj1, obj2));

    public static void Information(this IUnitLogger logger, ulong hash, object obj1, object obj2)
        => logger.Information(HashedString.Get(hash, obj1, obj2));

    public static void Warning(this IUnitLogger logger, ulong hash, object obj1, object obj2)
        => logger.Warning(HashedString.Get(hash, obj1, obj2));

    public static void Error(this IUnitLogger logger, ulong hash, object obj1, object obj2)
        => logger.Error(HashedString.Get(hash, obj1, obj2));

    public static void Fatal(this IUnitLogger logger, ulong hash, object obj1, object obj2)
        => logger.Fatal(HashedString.Get(hash, obj1, obj2));
}
