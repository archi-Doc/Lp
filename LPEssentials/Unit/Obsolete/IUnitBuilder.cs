// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;

namespace Arc.Unit.Obsolete;

public interface IUnitBuilder
{
    public UnitBuilder Configure(Action<UnitBuilderContext> configureDelegate);
}
