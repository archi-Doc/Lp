// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public class CrystalizerClass
{// tempcode
    public CrystalizerClass()
    {
    }

    public ICrystal<T> Create<T>()
        where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>
        => new CrystalImpl<T>(this);
}
