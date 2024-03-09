// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public interface INetsphereUnitContext
{
    void AddService<TService>()
        where TService : INetService;
}
