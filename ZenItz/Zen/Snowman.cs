// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

internal class Snowman
{
    public Snowman()
    {
    }

    public async Task<ZenDataResult> Load(SnowFlakeIdSegment idSegment)
    {
        return new ZenDataResult(ZenResult.Success, ByteArrayPool.ReadOnlyMemoryOwner.Empty);
    }
}
