// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData;

namespace CrystalData;

public interface IZenInternal
{
    DataConstructor Constructor { get; }

    ZenOptions Options { get; }

    Storage Storage { get; }

    HimoGoshujinClass HimoGoshujin { get; }
}
