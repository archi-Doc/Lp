// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public enum Crystalization
{
    None,
    Manual,
    Periodic,
    Instant,
}

public record CrystalConfiguration
{
    public CrystalConfiguration()
    {
    }

    public CrystalConfiguration(Crystalization crystalization)
    {
        this.Crystalization = crystalization;
    }

    public Crystalization Crystalization { get; init; }

    public TimeSpan Interval { get; init; }
}
