// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Unit;

public interface IUnitBuilder
{
    public UnitBuilder Configure(Action<UnitBuilderContext> configureDelegate);
}
