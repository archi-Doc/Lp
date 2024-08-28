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

    /// <summary>
    /// Adds a network service to the Netsphere unit context.
    /// </summary>
    /// <typeparam name="TService">The type of the network service to add.</typeparam>
    void INetsphereUnitContext.AddNetService<TService>()
    {
        this.services.Add(typeof(TService));
    }

    void INetsphereUnitContext.AddNetService<TService, TAgent>()
    {
        this.ServiceToAgent.TryAdd(typeof(TService), typeof(TAgent));
    }

    public FrozenSet<Type> Services { get; private set; } = FrozenSet<Type>.Empty;

    internal Dictionary<Type, Type> ServiceToAgent { get; } = new();

    private readonly HashSet<Type> services = new();
}
