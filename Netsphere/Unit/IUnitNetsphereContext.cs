// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public interface IUnitNetsphereContext
{
    void AddService<TService>()
        where TService : INetService;
}
