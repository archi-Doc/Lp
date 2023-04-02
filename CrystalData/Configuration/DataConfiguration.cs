// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public enum Crystalization
{
    None,
    Manual,
    Periodic,
    Instant,
}

public record DataConfiguration
{
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromHours(1);

    public static readonly DataConfiguration Default = new();

    public DataConfiguration()
    {
        this.Crystalization = Crystalization.None;
        this.FilerConfiguration = EmptyFilerConfiguration.Default;
    }

    public DataConfiguration(Crystalization crystalization, FilerConfiguration filerConfiguration)
    {
        this.Crystalization = crystalization;
        this.Interval = DefaultInterval;
        this.FilerConfiguration = filerConfiguration;
    }

    public DataConfiguration(TimeSpan interval, FilerConfiguration filerConfiguration)
    {
        this.Crystalization = Crystalization.Periodic;
        this.Interval = interval;
        this.FilerConfiguration = filerConfiguration;
    }

    public Crystalization Crystalization { get; init; }

    public TimeSpan Interval { get; init; }

    public FilerConfiguration FilerConfiguration { get; init; }
}
