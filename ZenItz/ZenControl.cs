// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using System.Threading.Tasks;
global using Arc.Threading;
global using Arc.Unit;
global using LP;
global using LP.Block;
global using Tinyhand;
global using ValueLink;
using Microsoft.Extensions.DependencyInjection;

namespace ZenItz;

public class ZenControl
{
    public class Builder : UnitBuilder<Unit>
    {// Builder class for customizing dependencies.
        public Builder()
            : base()
        {
            this.Configure(context =>
            {
                LPBase.Configure(context);

                // Main services
                context.AddSingleton<ZenControl>();
                context.AddSingleton<ZenOptions>();
                context.AddSingleton<Zen>();
                context.Services.Add(ServiceDescriptor.Transient(typeof(Zen.RootFlake), x => x.GetRequiredService<ZenControl>().Root));
                context.AddSingleton<Itz>();

                // Subcommands
                Subcommands.ZenDirSubcommand.Configure(context);
                Subcommands.ZenTempSubcommand.Configure(context);
            });
        }
    }

    public class Unit : BuiltUnit
    {// Unit class for customizing behaviors.
        public record Param();

        public Unit(UnitContext context)
            : base(context)
        {
        }
    }

    public ZenControl(UnitContext unitContext, Zen zen, ZenOptions options, Itz itz)
    {
        this.unitContext = unitContext;
        this.Zen = zen;
        this.Zen.Options = options;
        this.Root = this.Zen.Root;
        this.Itz = itz;
    }

    public Zen<TIdentifier> CreateZen<TIdentifier>(ZenOptions options)
        where TIdentifier : IEquatable<TIdentifier>, ITinyhandSerialize<TIdentifier>
    {
        return new Zen<TIdentifier>(options, this.unitContext.ServiceProvider.GetRequiredService<ILogger<Zen<TIdentifier>>>());
    }

    public Zen Zen { get; }

    public Zen.Flake Root { get; set; }

    public Itz Itz { get; }

    public bool ExaltationOfIntegrality { get; } = true; // by Baxter.

    private readonly UnitContext unitContext;
}
