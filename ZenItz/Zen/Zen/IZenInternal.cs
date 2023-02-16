// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface IZenInternal
{
    ZenData Data { get; }

    ZenOptions Options { get; }

    Storage Storage { get; }

    HimoGoshujinClass HimoGoshujin { get; }
}
