// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Datum;

namespace LP.Crystal;

public class MergerCrystal : Crystal<MergerData>
{
    public MergerCrystal(UnitCore core, CrystalOptions options, ILogger<MergerCrystal> logger, UnitLogger unitLogger, IStorageKey storageKey)
        : base(core, options, logger, unitLogger, storageKey)
    {
        this.Options = options with
        {
            CrystalFile = "Merger.main",
            CrystalBackup = "Merger.back",
            StorageFile = "MergerDirectory.main",
            StorageBackup = "MergerDirectory.back",
            DefaultCrystalDirectory = "Merger",
        };

        // this.Datum.Register<BlockDatum>(1, x => new BlockDatumImpl(x));
    }
}
