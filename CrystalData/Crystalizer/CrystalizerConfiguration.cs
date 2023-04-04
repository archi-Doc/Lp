// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record CrystalizerConfiguration
{
    public static readonly CrystalizerConfiguration Default = new CrystalizerConfiguration();

    public CrystalizerConfiguration()
        : this(new Dictionary<Type, CrystalConfiguration>(), new Dictionary<Type, BigCrystalConfiguration>(), Directory.GetCurrentDirectory())
    {
    }

    internal CrystalizerConfiguration(Dictionary<Type, CrystalConfiguration> crystalConfigurations, Dictionary<Type, BigCrystalConfiguration> bigCrystalConfigurations, string rootPath)
    {
        this.CrystalConfigurations = crystalConfigurations;
        this.BigCrystalConfigurations = bigCrystalConfigurations;
        this.RootPath = rootPath;
    }

    public bool EnableLogger { get; init; }

    public string RootPath { get; init; }

    public Dictionary<Type, CrystalConfiguration> CrystalConfigurations { get; }

    public Dictionary<Type, BigCrystalConfiguration> BigCrystalConfigurations { get; }
}

/*public record CrystalizerOptions
{
    public CrystalizerOptions(string rootPath)
    {
        this.RootPath = rootPath;
    }

    public bool EnableLogger { get; init; }

    public string RootPath { get; init; }
}*/
