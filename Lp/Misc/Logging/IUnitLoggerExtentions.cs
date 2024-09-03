﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp;

public static class IUnitLoggerExtentions
{
    public static void Log(this Arc.Unit.ILogWriter logger, ulong hash)
        => logger.Log(HashedString.Get(hash));

    public static void Log(this Arc.Unit.ILogWriter logger, ulong hash, object obj1)
        => logger.Log(HashedString.Get(hash, obj1));

    public static void Log(this Arc.Unit.ILogWriter logger, ulong hash, object obj1, object obj2)
        => logger.Log(HashedString.Get(hash, obj1, obj2));
}
