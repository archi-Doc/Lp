// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

internal class HimoTaskCore : TaskCore
{
    public HimoTaskCore(ThreadCoreBase parent, HimoGoshujinClass himoGoshujin)
        : base(parent, Method, true)
    {
        this.himoGoshujin = himoGoshujin;
    }

    private static async Task Method(object? taskCore)
    {
        var core = (HimoTaskCore)taskCore!;
        while (!core.IsTerminated)
        {
            core.himoGoshujin.Unload();
            await core.Delay(HimoGoshujinClass.UnloadInterval);
        }
    }

    private HimoGoshujinClass himoGoshujin;
}
