// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Frozen;

namespace Netsphere;

internal class NetsphereUnitContext : INetsphereUnitContext, IUnitCustomContext
{
    void IUnitCustomContext.Configure(IUnitConfigurationContext context)
    {
        context.SetOptions(this);
    }

    void INetsphereUnitContext.AddNetService<TService, TAgent>()
    {
        this.ServiceToAgent.TryAdd(typeof(TService), typeof(TAgent));
    }

    internal Dictionary<Type, Type> ServiceToAgent { get; } = new();
}
