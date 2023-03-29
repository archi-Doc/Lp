// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public class CrystalizerOptions
{
    internal CrystalizerOptions(Dictionary<Type, CrystalConfiguration> typeToCrystalConfiguration)
    {
        this.TypeToCrystalConfiguration = typeToCrystalConfiguration;
    }

#pragma warning disable SA1401
    internal Dictionary<Type, CrystalConfiguration> TypeToCrystalConfiguration;
#pragma warning restore SA1401
}
