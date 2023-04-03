// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface ICrystalDataObsolete
{
    // ok
    DatumRegistry Datum { get; }

    // get only
    CrystalOptions Options { get; set; }

    // skip
    public bool Started { get; }

    // -> Crystalizer
    StorageGroup Storage { get; }

    HimoGoshujinClass Himo { get; }

    long MemoryUsage { get; }

    // -> Crystalizer
    Task<CrystalStartResult> StartAsync(CrystalStartParam param);

    // -> Crystalizer
    Task StopAsync(CrystalStopParam param);

    // -> Crystalizer
    Task Abort();
}
