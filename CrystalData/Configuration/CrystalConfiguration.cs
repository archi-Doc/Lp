// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record CrystalConfiguration
{
    public CrystalConfiguration(Action<DatumRegistry> registerDatum)
    {
        this.RegisterDatum = registerDatum;
    }

    internal Action<DatumRegistry> RegisterDatum { get; }
}
