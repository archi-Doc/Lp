// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1208 // System using directives should be placed before other using directives
#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace
global using System;
global using System.Net;
global using System.Threading.Tasks;
global using Arc.Threading;
global using BigMachines;
global using CrossChannel;
global using LP;
global using LP.Block;
global using LP.Options;
global using Tinyhand;
global using ValueLink;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using LP.Unit;
using LPEssentials.Radio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Netsphere.Responder;
using SimpleCommandLine;

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
                // Base
                context.TryAddSingleton<BigMachine<Identifier>>();

                // Main services
                context.AddSingleton<NetControl>();
                context.AddSingleton<NetBase>();
                context.AddSingleton<Terminal>();
                context.AddSingleton<EssentialNode>();
                context.AddSingleton<NetStatus>();
                context.AddTransient<Server>();
                context.AddTransient<NetService>(); // serviceCollection.RegisterDelegate(x => new NetService(container), Reuse.Transient);

                // Machines
                context.AddTransient<LP.Machines.EssentialNetMachine>();

                // Subcommands
                context.AddCommand(typeof(LP.Subcommands.NetTestSubcommand));

                // Unit
            });
        }
    }

    public class Unit : BuiltUnit
    {
        public record Param(bool EnableServer, Func<ServerContext> NewServerContext, Func<CallContext> NewCallContext, string NodeName, NetsphereOptions Options, bool AllowUnsafeConnection);

        public Unit(UnitParameter parameter)
            : base(parameter)
        {
            NetControl.serviceProvider = parameter.ServiceProvider;
        }

        public void RunStandalone(Param param)
        {
            var netBase = this.ServiceProvider.GetRequiredService<NetBase>();
            netBase.Initialize(param.EnableServer, param.NodeName, param.Options);
            netBase.AllowUnsafeConnection = param.AllowUnsafeConnection;

            var netControl = this.ServiceProvider.GetRequiredService<NetControl>();
            if (param.EnableServer)
            {
                netControl.SetupServer(param.NewServerContext, param.NewCallContext);
            }

            Logger.Configure(null);
            this.SendPrepare(new());
            Radio.SendAsync(new Message.StartAsync(ThreadCore.Root));
        }
    }

    public NetControl(UnitParameter parameter, NetBase netBase, BigMachine<Identifier> bigMachine, Terminal terminal, EssentialNode node, NetStatus netStatus)
        : base(parameter)
    {
        this.ServiceProvider = (IServiceProvider)serviceProvider;
        this.NewServerContext = () => new ServerContext();
        this.NewCallContext = () => new CallContext();
        this.NetBase = netBase;
        this.BigMachine = bigMachine; // Warning: Can't call BigMachine.TryCreate() in a constructor.

        this.Terminal = terminal;
        if (this.NetBase.NetsphereOptions.EnableAlternative)
        {
            this.Alternative = new(netBase, netStatus); // For debug
        }

        this.EssentialNode = node;
        this.NetStatus = netStatus;
    }

    public void Prepare(UnitMessage.Prepare message)
    {
        // Terminals
        this.Terminal.Initialize(false, this.NetBase.NodePrivateKey, this.NetBase.NodePrivateEcdh);
        if (this.Alternative != null)
        {
            this.Alternative.Initialize(true, NodePrivateKey.AlternativePrivateKey, NodeKey.FromPrivateKey(NodePrivateKey.AlternativePrivateKey)!);
            this.Alternative.Port = NodeAddress.Alternative.Port;
        }

        // Responders
        DefaultResponder.Register(this);

        // Machines
        this.BigMachine.CreateNew<LP.Machines.EssentialNetMachine.Interface>(Identifier.Zero);
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

        static async Task InvokeServer(ServerTerminal terminal)
        {
            var server = serviceProvider.GetRequiredService<Server>();
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

    public IServiceProvider ServiceProvider { get; }

    public Func<ServerContext> NewServerContext { get; private set; }

    public Func<CallContext> NewCallContext { get; private set; }

    public NetBase NetBase { get; }

    public BigMachine<Identifier> BigMachine { get; }

    public MyStatus MyStatus => this.Terminal.MyStatus;

    public NetStatus NetStatus { get; }

    public Terminal Terminal { get; }

    public EssentialNode EssentialNode { get; }

    public Terminal? Alternative { get; }

    internal ConcurrentDictionary<ulong, INetResponder> Responders { get; } = new();

    private static IServiceProvider serviceProvider = default!;

    private void Dump()
    {
        Logger.Default.Information($"Dump:");
        Logger.Default.Information($"MyStatus: {this.MyStatus.Type}");
    }
}
