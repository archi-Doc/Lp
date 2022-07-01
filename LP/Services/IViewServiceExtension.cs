// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Services;

public static class ViewServiceExtention
{
    public static Task<bool> RequestYesOrNo(this IViewService viewService, ulong hash)
        => viewService.RequestYesOrNo(HashedString.Get(hash));

    public static Task<bool> RequestYesOrNo(this IViewService viewService, ulong hash, object obj1)
        => viewService.RequestYesOrNo(HashedString.Get(hash, obj1));

    public static Task<bool> RequestYesOrNo(this IViewService viewService, ulong hash, object obj1, object obj2)
        => viewService.RequestYesOrNo(HashedString.Get(hash, obj1, obj2));

    public static Task<string?> RequestString(this IViewService viewService, ulong hash)
        => viewService.RequestString(HashedString.Get(hash));

    public static Task<string?> RequestString(this IViewService viewService, ulong hash, object obj1)
        => viewService.RequestString(HashedString.Get(hash, obj1));

    public static Task<string?> RequestString(this IViewService viewService, ulong hash, object obj1, object obj2)
        => viewService.RequestString(HashedString.Get(hash, obj1, obj2));
}
