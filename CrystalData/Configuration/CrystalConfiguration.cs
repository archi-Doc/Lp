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
    public static readonly CrystalConfiguration Default = new();

    public CrystalConfiguration()
    {
        this.Crystalization = Crystalization.None;
    }

    public CrystalConfiguration(Crystalization crystalization, FilerConfiguration fileConfiguration)
    {
        this.Crystalization = crystalization;
    }

    public CrystalConfiguration(TimeSpan interval, FilerConfiguration fileConfiguration)
    {
        this.Crystalization = Crystalization.Periodic;
        this.Interval = interval;
    }

    public Crystalization Crystalization { get; init; }

    public TimeSpan Interval { get; init; }
}
