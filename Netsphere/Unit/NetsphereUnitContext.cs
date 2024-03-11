// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Frozen;

namespace Netsphere;

internal class NetsphereUnitContext : INetsphereUnitContext, IUnitCustomContext
{
    void IUnitCustomContext.Configure(IUnitConfigurationContext context)
    {
        this.Services = this.services.ToFrozenSet();
        context.SetOptions(this);
    }

    void INetsphereUnitContext.AddService<TService>()
    {
        this.services.Add(typeof(TService));
    }

    public FrozenSet<Type> Services { get; private set; } = FrozenSet<Type>.Empty;

    private readonly HashSet<Type> services = new();
}
