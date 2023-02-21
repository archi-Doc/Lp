// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface ICrystal
{
    DataConstructor Constructor { get; }

    CrystalOptions Options { get; set; }

    public bool Started { get; }

    Storage Storage { get; }
}
