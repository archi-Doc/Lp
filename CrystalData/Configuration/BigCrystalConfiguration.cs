// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record BigCrystalConfiguration
{
    public BigCrystalConfiguration(Action<DatumRegistry> registerDatum, BigCrystalOptions bigCrystalOptions)
    {
        this.RegisterDatum = registerDatum;
        this.BigCrystalOptions = bigCrystalOptions;
    }

    internal Action<DatumRegistry> RegisterDatum { get; }

    public BigCrystalOptions BigCrystalOptions { get; init; }
}
