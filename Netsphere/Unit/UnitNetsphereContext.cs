// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Frozen;

namespace Netsphere;

public class UnitNetsphereContext : IUnitNetsphereContext, IUnitCustomContext
{
    void IUnitCustomContext.Configure(IUnitConfigurationContext context)
    {
        this.Services = this.services.ToFrozenSet();
        context.SetOptions(this);
    }

    void IUnitNetsphereContext.AddService<TService>()
    {
        this.services.Add(typeof(TService));
    }

    public FrozenSet<Type> Services { get; private set; } = FrozenSet<Type>.Empty;

    private readonly HashSet<Type> services = new();
}
