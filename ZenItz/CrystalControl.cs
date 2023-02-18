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

namespace CrystalData;

public class CrystalControl
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
                context.AddSingleton<CrystalControl>();
                context.AddSingleton<CrystalOptions>();
                context.AddSingleton<Crystal>();
                context.Services.Add(ServiceDescriptor.Transient(typeof(Crystal.RootFlake), x => x.GetRequiredService<CrystalControl>().Root));
                context.AddSingleton<Itz>();

                // Subcommands
                Subcommands.CrystalDirSubcommand.Configure(context);
                Subcommands.CrystalTempSubcommand.Configure(context);
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

    public CrystalControl(UnitContext unitContext, Crystal zen, CrystalOptions options, Itz itz)
    {
        this.unitContext = unitContext;
        this.Zen = zen;
        this.Zen.Options = options;
        this.Root = this.Zen.Root;
        this.Itz = itz;
    }

    public Zen<TIdentifier> CreateZen<TIdentifier>(CrystalOptions options)
        where TIdentifier : IEquatable<TIdentifier>, IComparable<TIdentifier>, ITinyhandSerialize<TIdentifier>
    {
        return new Crystal<TIdentifier>(
            this.unitContext.ServiceProvider.GetRequiredService<UnitCore>(),
            options,
            this.unitContext.ServiceProvider.GetRequiredService<ILogger<Zen<TIdentifier>>>());
    }

    public Crystal Zen { get; }

    public Crystal.RootFlake Root { get; set; }

    public Itz Itz { get; }

    public bool ExaltationOfIntegrality { get; } = true; // by Baxter.

    private readonly UnitContext unitContext;
}
