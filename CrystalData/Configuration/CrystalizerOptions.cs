// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public class CrystalizerOptions
{
    internal CrystalizerOptions(Dictionary<Type, CrystalConfiguration> typeToCrystalConfiguration)
    {
        this.typeToCrystalConfiguration = typeToCrystalConfiguration;
    }

    private Dictionary<Type, CrystalConfiguration> typeToCrystalConfiguration;
}
