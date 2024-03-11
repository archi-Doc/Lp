﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System.Net;
global using Arc.Crypto;
global using Arc.Threading;
global using Arc.Unit;
global using BigMachines;
global using Tinyhand;
global using ValueLink;
using Microsoft.Extensions.DependencyInjection;
using Netsphere.Logging;
using Netsphere.Machines;
using Netsphere.Misc;
using Netsphere.Responder;
using Netsphere.Stats;

namespace Netsphere;

public class NetControl : UnitBase, IUnitPreparable
{
    public class Builder : UnitBuilder<Unit>
    {
        public Builder()
            : base()
        {
            this.Configure(context =>
            {
                // Main services
                context.AddSingleton<NetControl>();
                context.AddSingleton<NetBase>();
                context.AddSingleton<EssentialAddress>();
                context.AddSingleton<NetStats>();
                context.AddSingleton<NtpCorrection>();
                context.AddSingleton<NetTerminal>();
                context.AddSingleton<ServiceControl>();

                // Stream logger
                context.Services.Add(ServiceDescriptor.Singleton(typeof(IdFileLogger<>), typeof(IdFileLoggerFactory<>)));
                context.TryAddSingleton<IdFileLoggerOptions>();

                // Machines
                // context.AddTransient<EssentialNetMachine>();
                context.AddTransient<NtpMachine>();
                context.AddTransient<NetStatsMachine>();

                var customContext = context.GetCustomContext<NetsphereUnitContext>();
                foreach (var x in this.actions)
                {
                    x(customContext);
                }
            });
        }

        public Builder ConfigureSerivice(Action<INetsphereUnitContext> @delegate)
        {
            this.actions.Add(@delegate);
            return this;
        }

        public new Builder AddBuilder(UnitBuilder unitBuilder)
        {
            base.AddBuilder(unitBuilder);
            return this;
        }

        public new Builder Preload(Action<IUnitPreloadContext> @delegate)
        {
            base.Preload(@delegate);
            return this;
        }

        public new Builder Configure(Action<IUnitConfigurationContext> configureDelegate)
        {
            base.Configure(configureDelegate);
            return this;
        }

        public new Builder SetupOptions<TOptions>(Action<IUnitSetupContext, TOptions> @delegate)
            where TOptions : class
        {
            base.SetupOptions(@delegate);
            return this;
        }

        private readonly List<Action<INetsphereUnitContext>> actions = new();
    }

    public class Unit : BuiltUnit
    {
        public Unit(UnitContext context)
            : base(context)
        {
        }

        public async Task Run(NetOptions options, bool allowUnsafeConnection, Func<ServerConnection, ServerConnectionContext>? newServerConnectionContext = null, Func<ClientConnection, ClientConnectionContext>? newClientConnectionContext = null)
        {
            var netBase = this.Context.ServiceProvider.GetRequiredService<NetBase>();
            netBase.SetOptions(options);
            netBase.AllowUnsafeConnection = allowUnsafeConnection;

            if (newServerConnectionContext is not null)
            {
                netBase.NewServerConnectionContext = newServerConnectionContext;
            }

            if (newClientConnectionContext is not null)
            {
                netBase.NewClientConnectionContext = newClientConnectionContext;
            }

            var netControl = this.Context.ServiceProvider.GetRequiredService<NetControl>();
            this.Context.SendPrepare(new());
            await this.Context.SendStartAsync(new(ThreadCore.Root)).ConfigureAwait(false);
        }

        public Task Terminate()
            => this.Context.SendTerminateAsync(new());
    }

    public NetControl(UnitContext context, UnitLogger unitLogger, NetBase netBase, NetStats netStats, NetTerminal netTerminal)
        : base(context)
    {
        this.unitLogger = unitLogger;
        this.ServiceProvider = context.ServiceProvider;
        this.NetBase = netBase;
        this.NetStats = netStats;
        this.Responders = new();
        this.Services = new();

        var netsphereContext = context.ServiceProvider.GetRequiredService<NetsphereUnitContext>();
        foreach (var x in netsphereContext.Services)
        {
            this.Services.Register(x);
        }

        this.NetTerminal = netTerminal;
        this.NetTerminal.Initialize(this.Responders, this.Services, false);
        if (this.NetBase.NetOptions.EnableAlternative)
        {// For debugging
            this.Alternative = new(context, unitLogger, netBase, netStats);
            this.Alternative.Initialize(this.Responders, this.Services, true);
        }
    }

    #region FieldAndProperty

    public NetBase NetBase { get; }

    public NetStats NetStats { get; }

    public ResponderControl Responders { get; }

    public ServiceControl Services { get; }

    public NetTerminal NetTerminal { get; }

    public NetTerminal? Alternative { get; }

    public Func<ServerConnection, ServerConnectionContext> NewServerConnectionContext
    {
        get => this.NetBase.NewServerConnectionContext;
        set => this.NetBase.NewServerConnectionContext = value;
    }

    public Func<ClientConnection, ClientConnectionContext> NewClientConnectionContext
    {
        get => this.NetBase.NewClientConnectionContext;
        set => this.NetBase.NewClientConnectionContext = value;
    }

    internal IServiceProvider ServiceProvider { get; }

    private UnitLogger unitLogger;

    #endregion

    void IUnitPreparable.Prepare(UnitMessage.Prepare message)
    {
    }
}
