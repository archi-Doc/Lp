// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public class SnowmanControl
{
    public static readonly SnowmanControl Instance = new();

    public SnowmanControl()
    {
    }

    public int GetFlakeId()
    {
        return 0;
    }

    public async Task<ZenDataResult> TryLoadPrimary(SnowFlakeIdSegment idSegment, Identifier identifier)
    {
        return new(ZenResult.Success);
    }

    private Snowman[] flakeArray;
}
