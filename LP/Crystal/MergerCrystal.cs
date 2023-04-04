// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Datum;

namespace LP.Crystal;

public class MergerCrystal : BigCrystalImpl<MergerData>
{
    public MergerCrystal(Crystalizer crystalizer)
        : base(crystalizer)
    {
        // tempcode
        /*this.Options = options with
        {
            CrystalFile = "Merger.main",
            CrystalBackup = "Merger.back",
            StorageFile = "MergerDirectory.main",
            StorageBackup = "MergerDirectory.back",
            DefaultCrystalDirectory = "Merger",
        };*/

        // this.Datum.Register<BlockDatum>(1, x => new BlockDatumImpl(x));
    }
}
