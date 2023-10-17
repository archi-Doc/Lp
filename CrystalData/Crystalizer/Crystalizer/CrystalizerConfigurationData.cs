// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject(ImplicitKeyAsName = true, IncludePrivateMembers = true)]
public partial class CrystalizerConfigurationData
{
    public CrystalizerConfigurationData()
    {
    }

    public Dictionary<string, CrystalConfiguration> CrystalConfigurations { get; private set; } = default!;
}
