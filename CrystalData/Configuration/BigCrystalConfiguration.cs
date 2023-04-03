// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record BigCrystalConfiguration
{
    public BigCrystalConfiguration(Action<DatumRegistry> registerDatum)
    {
        this.RegisterDatum = registerDatum;
    }

    internal Action<DatumRegistry> RegisterDatum { get; }
}
