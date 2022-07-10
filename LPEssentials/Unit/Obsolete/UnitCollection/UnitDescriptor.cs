// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit.Obsolete;

public class UnitDescriptor
{
    public UnitDescriptor(Type unitType, bool createInstance)
    {
        this.UnitType = unitType;
        this.CreateInstance = createInstance;
    }

    public Type UnitType { get; }

    public bool CreateInstance { get; }
}
