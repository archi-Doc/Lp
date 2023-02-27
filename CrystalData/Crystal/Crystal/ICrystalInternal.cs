// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface ICrystalInternal
{
    DatumConstructor Datum { get; }

    CrystalOptions Options { get; }

    Storage Storage { get; }

    HimoGoshujinClass HimoGoshujin { get; }
}
