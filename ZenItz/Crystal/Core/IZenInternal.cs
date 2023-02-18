// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData;

namespace ZenItz.Crystal.Core;

public interface IZenInternal
{
    DataConstructor Constructor { get; }

    CrystalOptions Options { get; }

    Storage Storage { get; }

    HimoGoshujinClass HimoGoshujin { get; }
}
