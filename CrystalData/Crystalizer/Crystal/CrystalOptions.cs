// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record CrystalOptions
{
    public static readonly CrystalOptions Default = new CrystalOptions();

    public CrystalOptions()
        : this(new Dictionary<Type, CrystalConfiguration>(), new Dictionary<Type, BigCrystalConfiguration>(), Directory.GetCurrentDirectory())
    {
    }

    internal CrystalOptions(Dictionary<Type, CrystalConfiguration> crystalConfigurations, Dictionary<Type, BigCrystalConfiguration> bigCrystalConfigurations, string rootPath)
    {
        this.CrystalConfigurations = crystalConfigurations;
        this.BigCrystalConfigurations = bigCrystalConfigurations;
        this.RootPath = rootPath;
    }

    public bool EnableLogger { get; init; } = true; // tempcode

    public string RootPath { get; }

    public Dictionary<Type, CrystalConfiguration> CrystalConfigurations { get; }

    public Dictionary<Type, BigCrystalConfiguration> BigCrystalConfigurations { get; }
}
