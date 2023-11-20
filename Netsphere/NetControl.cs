// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1208 // System using directives should be placed before other using directives
#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System.Net;
global using Arc.Crypto;
global using Arc.Threading;
global using Arc.Unit;
global using BigMachines;
global using Tinyhand;
global using ValueLink;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Netsphere.Crypto;
using Netsphere.Logging;
using Netsphere.Machines;
using Netsphere.Misc;
using Netsphere.Responder;
using Netsphere.Stats;

namespace Netsphere;

public class NetControl : UnitBase, IUnitPreparable
{
    public const int MaxPayload = 1432; // 1432 bytes
    public const int MaxDataSize = 4 * 1024 * 1024; // 4 MB
    public const int MinPort = 49152; // Ephemeral port 49152 - 60999
    public const int MaxPort = 60999;

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
                context.AddSingleton<Terminal>();
                context.AddSingleton<EssentialAddress>();
                context.AddSingleton<NetStats>();
                context.AddTransient<Server>();
                context.AddSingleton<NtpCorrection>();
                // context.Services.Add(new ServiceDescriptor(typeof(NetService), x => new NetService(x), ServiceLifetime.Transient));
                // context.AddTransient<NetService>(); // serviceCollection.RegisterDelegate(x => new NetService(container), Reuse.Transient);

                // Stream logger
                context.Services.Add(ServiceDescriptor.Singleton(typeof(StreamLogger<>), typeof(StreamLoggerFactory<>)));
                context.TryAddSingleton<StreamLoggerOptions>();

                // Machines
                // context.AddTransient<EssentialNetMachine>();
                context.AddTransient<NtpMachine>();
                context.AddTransient<NetStatsMachine>();

                // Subcommands
                context.AddSubcommand(typeof(LP.Subcommands.NetTestSubcommand));
                context.AddSubcommand(typeof(LP.Subcommands.NetCleanSubcommand));
            });
        }
    }

    public class Unit : BuiltUnit
    {
        public record Param(bool EnableServer, Func<ServerContext> NewServerContext, Func<CallContext> NewCallContext, string NodeName, NetsphereOptions Options, bool AllowUnsafeConnection);

        public Unit(UnitContext context)
            : base(context)
        {
        }

        public async Task RunStandalone(Param param)
        {
            var netBase = this.Context.ServiceProvider.GetRequiredService<NetBase>();
            netBase.SetParameter(param.EnableServer, param.NodeName, param.Options);
            netBase.AllowUnsafeConnection = param.AllowUnsafeConnection;

            var netControl = this.Context.ServiceProvider.GetRequiredService<NetControl>();
            if (param.EnableServer)
            {
                netControl.SetupServer(param.NewServerContext, param.NewCallContext);
            }

            this.Context.SendPrepare(new());
            await this.Context.SendRunAsync(new(ThreadCore.Root)).ConfigureAwait(false);
        }
    }

    public NetControl(UnitContext context, UnitLogger unitLogger, NetBase netBase, NetStats netStats)
        : base(context)
    {
        this.unitLogger = unitLogger;
        this.ServiceProvider = context.ServiceProvider;
        this.NewServerContext = () => new ServerContext();
        this.NewCallContext = () => new CallContext();
        this.NetBase = netBase;

        this.Terminal = new(false, context, unitLogger, netBase, netStats);
        this.TerminalObsolete = new(context, unitLogger, netBase, netStats);
        if (this.NetBase.NetsphereOptions.EnableAlternative)
        {// For debugging
            this.Alternative = new(true, context, unitLogger, netBase, netStats);
            this.AlternativeObsolete = new(context, unitLogger, netBase, netStats);
        }
    }

    public void Prepare(UnitMessage.Prepare message)
    {
        // Terminals
        this.TerminalObsolete.Initialize(false, this.NetBase.NodePrivateKey);
        if (this.AlternativeObsolete != null)
        {
            this.AlternativeObsolete.Initialize(true, NodePrivateKey.AlternativePrivateKey);
            this.AlternativeObsolete.Port = NetAddress.Alternative.Port;
        }

        // Responders
        DefaultResponder.Register(this.TerminalObsolete);
    }

    public void SetupServer(Func<ServerContext>? newServerContext = null, Func<CallContext>? newCallContext = null)
    {
        if (newServerContext != null)
        {
            this.NewServerContext = newServerContext;
        }

        if (newCallContext != null)
        {
            this.NewCallContext = newCallContext;
        }

        this.TerminalObsolete.SetInvokeServerDelegate(InvokeServer);
        this.AlternativeObsolete?.SetInvokeServerDelegate(InvokeServer);

        async Task InvokeServer(ServerTerminal terminal)
        {
            var server = this.ServiceProvider.GetRequiredService<Server>();
            try
            {
                await server.Process(terminal).ConfigureAwait(false);
            }
            finally
            {
                terminal.Dispose();
            }
        }
    }

    public Func<ServerContext> NewServerContext { get; private set; }

    public Func<CallContext> NewCallContext { get; private set; }

    public NetBase NetBase { get; }

    public NetTerminal Terminal { get; }

    public NetTerminal? Alternative { get; }

    public Terminal TerminalObsolete { get; }

    public Terminal? AlternativeObsolete { get; }

    internal IServiceProvider ServiceProvider { get; }

    private UnitLogger unitLogger;

    private void Dump(ILog logger)
    {
        logger.Log($"Dump:");
    }
}
