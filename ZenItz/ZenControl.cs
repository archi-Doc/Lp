// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using System;
global using System.Net;
global using System.Threading.Tasks;
global using Arc.Threading;
global using Arc.Unit;
global using LP;
global using LP.Block;
global using Tinyhand;
global using ValueLink;

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

    public ZenControl(Zen zen, ZenOptions options, Itz itz)
    {
        this.Zen = zen;
        this.Zen.Options = options;
        this.Itz = itz;
    }

    public Zen Zen { get; }

    public Itz Itz { get; }

    public bool ExaltationOfIntegrality { get; } = true; // by Baxter.
}
