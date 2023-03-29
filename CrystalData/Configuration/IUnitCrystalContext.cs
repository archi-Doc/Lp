// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IUnitCrystalContext
{
    void TryAdd<T>(CrystalConfiguration crystalConfiguration)
        where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>;
}
