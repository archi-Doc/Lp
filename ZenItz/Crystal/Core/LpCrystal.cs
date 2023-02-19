// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public class LpCrystal : Crystal<LpCrystalData>
{
    public LpCrystal(UnitCore core, CrystalOptions options, ILogger<LpCrystal> logger)
        : base(core, options, logger)
    {
    }

    public LpData Data => this.Root.Data;
}

[TinyhandObject]
public partial class LpCrystalData : BaseData
{
    public LpCrystalData(ICrystalInternal crystal, BaseData? parent)
        : base(crystal, parent)
    {
        this.Data = new(crystal, parent);
    }

    internal LpCrystalData()
    {
        this.Data = new();
    }

    [Key(3)]
    public LpData Data { get; private set; }

    protected override IEnumerator<BaseData> EnumerateInternal()
    {
        yield return this.Data;
    }
}
