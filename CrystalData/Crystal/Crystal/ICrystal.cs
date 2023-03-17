// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface ICrystal
{
    DatumRegistry Datum { get; }

    CrystalOptions Options { get; set; }

    public bool Started { get; }

    Storage Storage { get; }

    HimoGoshujinClass Himo { get; }

    long MemoryUsage { get; }

    Task<CrystalStartResult> StartAsync(CrystalStartParam param);

    Task StopAsync(CrystalStopParam param);

    Task Abort();
}
