// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Crystal;

public class LpCrystal : Crystal<LpRootData>
{
    public LpCrystal(UnitCore core, CrystalOptions options, ILogger<LpCrystal> logger, UnitLogger unitLogger)
        : base(core, options, logger, unitLogger)
    {
        // this.Datum.Register<BlockDatum>(1, x => new BlockDatumImpl(x));
    }

    public LpData Data => this.Root.Data;
}

[TinyhandObject]
public partial class LpRootData : BaseData
{
    public LpRootData(ICrystalInternal crystal, BaseData? parent)
        : base(crystal, parent)
    {
        this.Data = new(crystal, parent, default);
    }

    internal LpRootData()
    {
        this.Data = new();
    }

    [Key(4)]
    public LpData Data { get; private set; }

    protected override IEnumerator<BaseData> EnumerateInternal()
    {
        yield return this.Data;
    }
}
