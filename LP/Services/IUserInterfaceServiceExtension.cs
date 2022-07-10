// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Services;

public static class IUserInterfaceServiceExtention
{
    public static Task<bool?> RequestYesOrNo(this IUserInterfaceService viewService, ulong hash)
        => viewService.RequestYesOrNo(HashedString.Get(hash));

    public static Task<bool?> RequestYesOrNo(this IUserInterfaceService viewService, ulong hash, object obj1)
        => viewService.RequestYesOrNo(HashedString.Get(hash, obj1));

    public static Task<bool?> RequestYesOrNo(this IUserInterfaceService viewService, ulong hash, object obj1, object obj2)
        => viewService.RequestYesOrNo(HashedString.Get(hash, obj1, obj2));

    public static Task<string?> RequestString(this IUserInterfaceService viewService, ulong hash)
        => viewService.RequestString(HashedString.Get(hash));

    public static Task<string?> RequestString(this IUserInterfaceService viewService, ulong hash, object obj1)
        => viewService.RequestString(HashedString.Get(hash, obj1));

    public static Task<string?> RequestString(this IUserInterfaceService viewService, ulong hash, object obj1, object obj2)
        => viewService.RequestString(HashedString.Get(hash, obj1, obj2));

    public static Task<string?> RequestPassword(this IUserInterfaceService viewService, ulong hash)
        => viewService.RequestPassword(HashedString.Get(hash));

    public static Task<string?> RequestPassword(this IUserInterfaceService viewService, ulong hash, object obj1)
        => viewService.RequestPassword(HashedString.Get(hash, obj1));

    public static Task<string?> RequestPassword(this IUserInterfaceService viewService, ulong hash, object obj1, object obj2)
        => viewService.RequestPassword(HashedString.Get(hash, obj1, obj2));

    public static async Task<string?> RequestPasswordAndConfirm(this IUserInterfaceService viewService, ulong hash, ulong hash2)
    {
        string? password;
        string? confirm;

        do
        {
            password = await viewService.RequestPassword(hash);
            if (password == null)
            {
                return null;
            }
        }
        while (password == string.Empty);

        while (true)
        {
            confirm = await viewService.RequestPassword(hash2);
            if (confirm == null)
            {
                return null;
            }
            else if (confirm == string.Empty)
            {
            }
            else if (password != confirm)
            {
                Logger.Default.Warning(Hashed.Dialog.Password.NotMatch);
            }
            else
            {
                break;
            }
        }

        return password;
    }
}
