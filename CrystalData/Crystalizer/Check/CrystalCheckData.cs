// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;

namespace CrystalData.Check;

[TinyhandObject]
internal partial class CrystalCheckData
{
    public CrystalCheckData()
    {
    }

    [Key(0)]
    public ConcurrentDictionary<DataAndConfigurationIdentifier, int> DataAndConfigurations { get; private set; } = default!;
}
