// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Crystal;

public class MergerCrystal : Crystal<LpData>
{
    public MergerCrystal(UnitCore core, CrystalOptions options, ILogger<MergerCrystal> logger)
        : base(core, options, logger)
    {
        this.Options = options with
        {
            CrystalFile = "Merger.main",
            CrystalBackup = "Merger.back",
            CrystalDirectoryFile = "MergerDirectory.main",
            CrystalDirectoryBackup = "MergerDirectory.back",
            DefaultCrystalDirectory = "Merger",
        };

        this.Constructor.Register<BlockDatum>(x => new BlockDatumImpl(x));
    }
}
