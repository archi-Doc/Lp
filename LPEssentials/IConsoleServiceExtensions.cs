// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Services;

public static class IConsoleServiceExtensions
{
    public static void Write(this IConsoleService service, ulong hash)
        => service.Write(HashedString.Get(hash));

    public static void Write(this IConsoleService service, ulong hash, object obj1)
        => service.Write(HashedString.Get(hash, obj1));

    public static void Write(this IConsoleService service, ulong hash, object obj1, object obj2)
        => service.Write(HashedString.Get(hash, obj1, obj2));

    public static void WriteLine(this IConsoleService service, ulong hash)
        => service.WriteLine(HashedString.Get(hash));

    public static void WriteLine(this IConsoleService service, ulong hash, object obj1)
        => service.WriteLine(HashedString.Get(hash, obj1));

    public static void WriteLine(this IConsoleService service, ulong hash, object obj1, object obj2)
        => service.WriteLine(HashedString.Get(hash, obj1, obj2));
}
