// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp;

public static class IUserInterfaceServiceExtention
{
    public static void WriteLine(this IUserInterfaceService service, ulong hash)
        => service.WriteLine(HashedString.Get(hash));

    public static void WriteLine(this IUserInterfaceService service, ulong hash, object obj1)
        => service.WriteLine(HashedString.Get(hash, obj1));

    public static void WriteLine(this IUserInterfaceService service, ulong hash, object obj1, object obj2)
        => service.WriteLine(HashedString.Get(hash, obj1, obj2));

    public static Task<bool?> RequestYesOrNo(this IUserInterfaceService viewService, ulong hash)
        => viewService.RequestYesOrNo(HashedString.Get(hash));

    public static Task<bool?> RequestYesOrNo(this IUserInterfaceService viewService, ulong hash, object obj1)
        => viewService.RequestYesOrNo(HashedString.Get(hash, obj1));

    public static Task<bool?> RequestYesOrNo(this IUserInterfaceService viewService, ulong hash, object obj1, object obj2)
        => viewService.RequestYesOrNo(HashedString.Get(hash, obj1, obj2));

    public static Task<string?> RequestString(this IUserInterfaceService viewService, bool cancelOnEscape, ulong hash)
        => viewService.RequestString(cancelOnEscape, HashedString.Get(hash));

    public static Task<string?> RequestString(this IUserInterfaceService viewService, bool cancelOnEscape, ulong hash, object obj1)
        => viewService.RequestString(cancelOnEscape, HashedString.Get(hash, obj1));

    public static Task<string?> RequestString(this IUserInterfaceService viewService, bool cancelOnEscape, ulong hash, object obj1, object obj2)
        => viewService.RequestString(cancelOnEscape, HashedString.Get(hash, obj1, obj2));

    public static Task<string?> RequestPassword(this IUserInterfaceService viewService, ulong hash)
        => viewService.RequestPassword(HashedString.Get(hash));

    public static Task<string?> RequestPassword(this IUserInterfaceService viewService, ulong hash, object obj1)
        => viewService.RequestPassword(HashedString.Get(hash, obj1));

    public static Task<string?> RequestPassword(this IUserInterfaceService viewService, ulong hash, object obj1, object obj2)
        => viewService.RequestPassword(HashedString.Get(hash, obj1, obj2));

    public static Task Notify(this IUserInterfaceService viewService, LogLevel level, ulong hash)
        => viewService.Notify(level, HashedString.Get(hash));

    public static Task Notify(this IUserInterfaceService viewService, LogLevel level, ulong hash, object obj1)
        => viewService.Notify(level, HashedString.Get(hash, obj1));

    public static Task Notify(this IUserInterfaceService viewService, LogLevel level, ulong hash, object obj1, object obj2)
        => viewService.Notify(level, HashedString.Get(hash, obj1, obj2));

    public static async Task<string?> RequestPasswordAndConfirm(this IUserInterfaceService viewService, ulong hash, ulong hash2)
    {
        string? password;
        string? confirm;

        do
        {
            password = await viewService.RequestPassword(hash).ConfigureAwait(false);
            if (password == null)
            {
                return null;
            }
            else if (password == string.Empty)
            {
                await viewService.Notify(LogLevel.Warning, Hashed.Dialog.Password.EmptyWarning).ConfigureAwait(false);
                var reply = await viewService.RequestYesOrNo(Hashed.Dialog.Password.EmptyConfirm).ConfigureAwait(false);
                if (reply != false)
                {// Empty password or abort
                    return password;
                }
            }
        }
        while (password == string.Empty);

        while (true)
        {
            confirm = await viewService.RequestPassword(hash2).ConfigureAwait(false);
            if (confirm == null)
            {
                return null;
            }
            else if (password != confirm)
            {
                await viewService.Notify(LogLevel.Warning, Hashed.Dialog.Password.NotMatch).ConfigureAwait(false);
            }
            else
            {
                break;
            }
        }

        return password;
    }
}
