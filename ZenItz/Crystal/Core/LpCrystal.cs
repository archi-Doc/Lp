// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public class LpCrystal : Crystal<LpRootData>
{
    public LpCrystal(UnitCore core, CrystalOptions options, ILogger<LpCrystal> logger)
        : base(core, options, logger)
    {
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

    [Key(3)]
    public LpData Data { get; private set; }

    protected override IEnumerator<BaseData> EnumerateInternal()
    {
        yield return this.Data;
    }
}
