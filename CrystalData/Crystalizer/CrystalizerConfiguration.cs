// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public class CrystalizerConfiguration
{
    internal CrystalizerConfiguration(Dictionary<Type, CrystalConfiguration> crystalConfigurations, Dictionary<Type, BigCrystalConfiguration> bigCrystalConfigurations)
    {
        this.CrystalConfigurations = crystalConfigurations;
        this.BigCrystalConfigurations = bigCrystalConfigurations;
    }

    public Dictionary<Type, CrystalConfiguration> CrystalConfigurations { get; }

    public Dictionary<Type, BigCrystalConfiguration> BigCrystalConfigurations { get; }
}
