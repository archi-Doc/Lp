// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Unit;

namespace LP.Unit.Obsolete;

public interface IUnitBuilder
{
    public UnitBuilder Configure(Action<UnitBuilderContext> configureDelegate);
}
