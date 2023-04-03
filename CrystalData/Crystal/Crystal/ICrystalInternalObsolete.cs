// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface ICrystalInternalObsolete
{
    DatumRegistry Datum { get; }

    CrystalOptions Options { get; }

    StorageGroup Storage { get; }

    HimoGoshujinClass HimoGoshujin { get; }
}
