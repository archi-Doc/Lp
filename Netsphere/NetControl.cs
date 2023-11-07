// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1208 // System using directives should be placed before other using directives
#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace
global using System.Net;
global using Arc.Threading;
global using Arc.Unit;
global using BigMachines;
global using LP;
global using LP.Block;
global using LP.Data;
global using Tinyhand;
global using ValueLink;
using CrystalData;
using System.Collections.Concurrent;
using LP.T3CS;
using Microsoft.Extensions.DependencyInjection;
using Netsphere.Logging;
using Netsphere.Machines;
using Netsphere.Responder;

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
                LPBase.Configure(context);

                // Main services
                context.AddSingleton<NetControl>();
                context.AddSingleton<NetBase>();
                context.AddSingleton<Terminal>();
                context.AddSingleton<EssentialNode>();
                context.AddSingleton<NetStatus>();
                context.AddTransient<Server>();
                // context.Services.Add(new ServiceDescriptor(typeof(NetService), x => new NetService(x), ServiceLifetime.Transient));
                // context.AddTransient<NetService>(); // serviceCollection.RegisterDelegate(x => new NetService(container), Reuse.Transient);

                // Stream logger
                context.Services.Add(ServiceDescriptor.Singleton(typeof(StreamLogger<>), typeof(StreamLoggerFactory<>)));
                context.TryAddSingleton<StreamLoggerOptions>();

                // Machines
                // context.AddTransient<EssentialNetMachine>();
                context.AddTransient<NtpMachine>();
                context.AddTransient<PublicIPMachine>();

                // Subcommands
                context.AddSubcommand(typeof(LP.Subcommands.NetTestSubcommand));
                context.AddSubcommand(typeof(LP.Subcommands.NetCleanSubcommand));
            });

            var crystalDataBuilder = new CrystalData.CrystalControl.Builder();
            /*crystalDataBuilder.ConfigureCrystal(context =>
            {
                context.AddCrystal<PublicIPMachine.Data>(new CrystalConfiguration() with
                {
                    SaveFormat = SaveFormat.Utf8,
                    FileConfiguration = new GlobalFileConfiguration("PublicIP.tinyhand"),
                    NumberOfHistoryFiles = 0,
                });
            });*/

            this.AddBuilder(crystalDataBuilder);
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

    public NetControl(UnitContext context, UnitLogger logger, NetBase netBase, Terminal terminal, EssentialNode node, NetStatus netStatus)
        : base(context)
    {
        this.logger = logger;
        this.ServiceProvider = context.ServiceProvider;
        this.NewServerContext = () => new ServerContext();
        this.NewCallContext = () => new CallContext();
        this.NetBase = netBase;

        this.Terminal = terminal;
        if (this.NetBase.NetsphereOptions.EnableAlternative)
        {
            this.Alternative = new(context, logger, netBase, netStatus); // For debug
        }

        this.EssentialNode = node;
        this.NetStatus = netStatus;
    }

    public void Prepare(UnitMessage.Prepare message)
    {
        // Terminals
        this.Terminal.Initialize(false, this.NetBase.NodePrivateKey);
        if (this.Alternative != null)
        {
            this.Alternative.Initialize(true, NodePrivateKey.AlternativePrivateKey);
            if (this.NetBase.NetsphereOptions.Port == NodeAddress.Alternative.Port)
            {
                NodeAddress.Alternative.SetPort((ushort)(this.Terminal.Port + 1));
            }

            this.Alternative.Port = NodeAddress.Alternative.Port;
        }

        // Responders
        DefaultResponder.Register(this);
    }

    public void CreateMachines()
    {
        // Machines
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

        this.Terminal.SetInvokeServerDelegate(InvokeServer);
        this.Alternative?.SetInvokeServerDelegate(InvokeServer);

        async Task InvokeServer(ServerTerminal terminal)
        {
            var server = this.ServiceProvider.GetRequiredService<Server>();
            terminal.Terminal.MyStatus.IncrementServerCount();
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

    public bool AddResponder(INetResponder responder)
    {
        return this.Responders.TryAdd(responder.GetDataId(), responder);
    }

    public Func<ServerContext> NewServerContext { get; private set; }

    public Func<CallContext> NewCallContext { get; private set; }

    public NetBase NetBase { get; }

    public MyStatus MyStatus => this.Terminal.MyStatus;

    public NetStatus NetStatus { get; }

    public Terminal Terminal { get; }

    public EssentialNode EssentialNode { get; }

    public Terminal? Alternative { get; }

    internal ConcurrentDictionary<ulong, INetResponder> Responders { get; } = new();

    internal IServiceProvider ServiceProvider { get; }

    private UnitLogger logger;

    private void Dump(ILog logger)
    {
        logger.Log($"Dump:");
        logger.Log($"MyStatus: {this.MyStatus.Type}");
    }
}
