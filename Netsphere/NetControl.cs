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
using DryIoc;
using LPEssentials.Radio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Netsphere.Responder;
using SimpleCommandLine;

namespace Netsphere;

public class NetControl
{
    public const int MaxPayload = 1432; // 1432 bytes
    public const int MaxDataSize = 4 * 1024 * 1024; // 4 MB
    public const int MinPort = 49152; // Ephemeral port 49152 - 60999
    public const int MaxPort = 60999;

    public static NetControlBuilder CreateBuilder()
    {
        return new NetControlBuilder();
    }

    public static void Register(Container container, List<Type>? commandList = null)
    {
        // Container instance
        serviceProvider = container;

        // Base
        if (!container.IsRegistered<BigMachine<Identifier>>())
        {
            container.RegisterDelegate(x => new BigMachine<Identifier>(container), Reuse.Singleton);
        }

        // Main services
        container.Register<NetControl>(Reuse.Singleton);
        container.Register<NetBase>(Reuse.Singleton);
        container.Register<Terminal>(Reuse.Singleton);
        container.Register<EssentialNode>(Reuse.Singleton);
        container.Register<NetStatus>(Reuse.Singleton);
        container.Register<Server>(Reuse.Transient);
        container.RegisterDelegate(x => new NetService(container), Reuse.Transient);

        // Machines
        container.Register<LP.Machines.EssentialNetMachine>();

        // Subcommands
        var commandTypes = new Type[]
        {
            typeof(LP.Subcommands.NetTestSubcommand),
        };

        commandList?.AddRange(commandTypes);
        foreach (var x in commandTypes)
        {
            container.Register(x, Reuse.Singleton);
        }
    }

    public static void Register(IServiceCollection serviceCollection, List<Type>? commandList = null)
    {
        // Base
        serviceCollection.TryAddSingleton<BigMachine<Identifier>>();

        // Main services
        serviceCollection.AddSingleton<NetControl>();
        serviceCollection.AddSingleton<NetBase>();
        serviceCollection.AddSingleton<Terminal>();
        serviceCollection.AddSingleton<EssentialNode>();
        serviceCollection.AddSingleton<NetStatus>();
        serviceCollection.AddTransient<Server>();
        serviceCollection.AddTransient<NetService>();
        // serviceCollection.RegisterDelegate(x => new NetService(container), Reuse.Transient);

        // Machines
        serviceCollection.AddTransient<LP.Machines.EssentialNetMachine>();

        // Subcommands
        var commandTypes = new Type[]
        {
            typeof(LP.Subcommands.NetTestSubcommand),
        };

        commandList?.AddRange(commandTypes);
        foreach (var x in commandTypes)
        {
            serviceCollection.AddSingleton(x);
        }
    }

    public static void SetServiceProvider(IServiceProvider provider)
    {
        serviceProvider = provider;
    }

    /*public static void QuickStart(bool enableServer, string nodeName, NetsphereOptions options, bool allowUnsafeConnection)
        => QuickStart<ServerContext>(enableServer, nodeName, options, allowUnsafeConnection, () => new CallContext());*/

    public static void QuickStart(bool enableServer, Func<ServerContext> newServerContext, Func<CallContext> newCallContext, string nodeName, NetsphereOptions options, bool allowUnsafeConnection)
    {
        var netBase = serviceProvider.GetRequiredService<NetBase>();
        netBase.Initialize(enableServer, nodeName, options);
        netBase.AllowUnsafeConnection = allowUnsafeConnection;

        var netControl = serviceProvider.GetRequiredService<NetControl>();
        if (enableServer)
        {
            netControl.SetupServer(newServerContext, newCallContext);
        }

        Logger.Configure(null);
        Radio.Send(new Message.Configure());
        Radio.SendAsync(new Message.StartAsync(ThreadCore.Root));
    }

    public NetControl(NetBase netBase, BigMachine<Identifier> bigMachine, Terminal terminal, EssentialNode node, NetStatus netStatus)
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

        Radio.Open<Message.Configure>(this.Configure);
    }

    public void Configure(Message.Configure message)
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
