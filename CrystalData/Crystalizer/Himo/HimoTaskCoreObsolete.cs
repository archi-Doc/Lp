// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

internal class HimoTaskCoreObsolete : TaskCore
{
    public HimoTaskCoreObsolete(ThreadCoreBase parent, HimoGoshujinClass himoGoshujin)
        : base(parent, Method, true)
    {
        this.himoGoshujin = himoGoshujin;
    }

    private static async Task Method(object? taskCore)
    {
        var core = (HimoTaskCoreObsolete)taskCore!;
        while (!core.IsTerminated)
        {
            core.himoGoshujin.Unload();
            await core.Delay(HimoGoshujinClass.UnloadInterval).ConfigureAwait(false);
        }
    }

    private HimoGoshujinClass himoGoshujin;
}
