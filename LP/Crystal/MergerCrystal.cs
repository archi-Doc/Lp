// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Datum;

namespace LP.Crystal;

public class MergerCrystal : Crystal<MergerData>
{
    public MergerCrystal(UnitCore core, CrystalOptions options, ILogger<MergerCrystal> logger, UnitLogger unitLogger)
        : base(core, options, logger, unitLogger)
    {
        this.Options = options with
        {
            CrystalFile = "Merger.main",
            CrystalBackup = "Merger.back",
            CrystalDirectoryFile = "MergerDirectory.main",
            CrystalDirectoryBackup = "MergerDirectory.back",
            DefaultCrystalDirectory = "Merger",
        };

        this.himo = new(10);
        this.Constructor.Register<BlockDatum>(x => new BlockDatumImpl(x));
    }

    private DataHimo himo;
}
