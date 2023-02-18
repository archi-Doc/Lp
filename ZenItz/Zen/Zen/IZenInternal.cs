// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface IZenInternal
{
    DataConstructor Constructor { get; }

    ZenOptions Options { get; }

    Storage Storage { get; }

    HimoGoshujinClass HimoGoshujin { get; }
}
