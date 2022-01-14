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
using Netsphere.Responder;
using SimpleCommandLine;

namespace Netsphere;

public class NetControl
{
    public const int MaxPayload = 1432; // 1432 bytes
    public const int MaxDataSize = 4 * 1024 * 1024; // 4 MB
    public const int MinPort = 49152; // Ephemeral port 49152 - 60999
    public const int MaxPort = 60999;

    public static void Register(Container container, List<Type> commandList)
    {
        // Container instance
        containerInstance = container;

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

        commandList.AddRange(commandTypes);
        foreach (var x in commandTypes)
        {
            container.Register(x, Reuse.Singleton);
        }
    }

    public static void QuickStart(bool enableServer, string nodeName, NetsphereOptions options, bool allowUnsafeConnection)
    {
        var netBase = containerInstance.Resolve<NetBase>();
        netBase.Initialize(enableServer, nodeName, options);
        netBase.AllowUnsafeConnection = allowUnsafeConnection;

        var netControl = containerInstance.Resolve<NetControl>();
        Logger.Configure(null);
        Radio.Send(new Message.Configure());
        var message = new Message.Start(ThreadCore.Root);
        Radio.Send(message);
        if (message.Abort)
        {
            Radio.Send(new Message.Stop());
            return;
        }
    }

    public NetControl(NetBase netBase, BigMachine<Identifier> bigMachine, Terminal terminal, EssentialNode node, NetStatus netStatus)
    {
        this.NetBase = netBase;
        this.BigMachine = bigMachine; // Warning: Can't call BigMachine.TryCreate() in a constructor.

        this.Terminal = terminal;
        if (this.NetBase.NetsphereOptions.EnableAlternative)
        {
            this.Alternative = new(netBase, netStatus); // For debug
        }

        this.SetServerTerminalDelegate(CreateServerTerminal);
        this.EssentialNode = node;
        this.NetStatus = netStatus;

        Radio.Open<Message.Configure>(this.Configure);
    }

    public void Configure(Message.Configure message)
    {
        // Terminals
        this.Terminal.Initialize(false, this.NetBase.NodePrivateEcdh);
        if (this.Alternative != null)
        {
            this.Alternative.Initialize(true, NodeKey.FromPrivateKey(NodePrivateKey.AlternativePrivateKey)!);
            this.Alternative.Port = NodeAddress.Alternative.Port;
        }

        // Responders
        DefaultResponder.Register(this);

        // Machines
        this.BigMachine.TryCreate<LP.Machines.EssentialNetMachine.Interface>(Identifier.Zero);
    }

    public void SetServerTerminalDelegate(Terminal.CreateServerTerminalDelegate @delegate)
    {
        this.Terminal.SetServerTerminalDelegate(@delegate);
        this.Alternative?.SetServerTerminalDelegate(@delegate);
    }

    public bool AddResponder(INetResponder responder)
    {
        return this.Responders.TryAdd(responder.GetDataId(), responder);
    }

    public NetBase NetBase { get; }

    public BigMachine<Identifier> BigMachine { get; }

    public MyStatus MyStatus => this.Terminal.MyStatus;

    public NetStatus NetStatus { get; }

    public Terminal Terminal { get; }

    public EssentialNode EssentialNode { get; }

    public Terminal? Alternative { get; }

    internal ConcurrentDictionary<ulong, INetResponder> Responders { get; } = new();

    private static Container containerInstance = default!;

    private static void CreateServerTerminal(ServerTerminal terminal)
    {
        Task.Run(async () =>
        {
            var server = containerInstance.Resolve<Server>();
            terminal.Terminal.MyStatus.IncrementServerCount();
            try
            {
                await server.Process(terminal).ConfigureAwait(false);
            }
            finally
            {
                terminal.Dispose();
            }
        });
    }

    private void Dump()
    {
        Logger.Default.Information($"Dump:");
        Logger.Default.Information($"MyStatus: {this.MyStatus.Type}");
    }
}
