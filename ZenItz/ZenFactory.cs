// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace ZenItz;

public class ZenFactory
{
    public ZenFactory(UnitContext unitContext)
    {
        this.unitContext = unitContext;
    }

    public Zen<TIdentifier> Create<TIdentifier>(ZenOptions options)
        where TIdentifier : IEquatable<TIdentifier>, ITinyhandSerialize<TIdentifier>
    {
        return new Zen<TIdentifier>(options, this.unitContext.ServiceProvider.GetRequiredService<ILogger<Zen<TIdentifier>>>());
    }

    private readonly UnitContext unitContext;
}
