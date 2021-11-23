// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using System;
global using System.Net;
global using System.Threading.Tasks;
global using Arc.Threading;
global using BigMachines;
global using CrossChannel;
global using LP;
global using LP.Net;
global using Tinyhand;
global using ValueLink;

namespace LP.Net;

public class Netsphere
{
    public delegate void CreateServerTerminalDelegate(NetTerminalServer terminal);

    public const int MaxPayload = 1432; // 1432 bytes
    public const int MinPort = 49152; // Ephemeral port 49152 - 60999
    public const int MaxPort = 60999;

    public Netsphere(BigMachine<Identifier> bigMachine, Information information, Private @private, Terminal terminal, EssentialNode node, NetStatus netStatus)
    {
        this.bigMachine = bigMachine; // Warning: Can't call BigMachine.TryCreate() in a constructor.
        this.information = information;
        this.@private = @private;
        this.Terminal = terminal;
        this.Alternative = new(information, netStatus); // For debug
        this.EssentialNode = node;
        this.NetStatus = netStatus;

        Radio.Open<Message.Configure>(this.Configure);
        Radio.Open<Message.Start>(this.Start);
    }

    public void Configure(Message.Configure message)
    {
        // Set port number
        if (this.information.ConsoleOptions.NetsphereOptions.Port < Netsphere.MinPort ||
            this.information.ConsoleOptions.NetsphereOptions.Port > Netsphere.MaxPort)
        {
            var showWarning = false;
            if (this.information.ConsoleOptions.NetsphereOptions.Port != 0)
            {
                showWarning = true;
            }

            this.information.ConsoleOptions.NetsphereOptions.Port = Random.Pseudo.NextInt(Netsphere.MinPort, Netsphere.MaxPort + 1);
            if (showWarning)
            {
                Logger.Default.Warning($"Port number must be between {Netsphere.MinPort} and {Netsphere.MaxPort}");
                Logger.Default.Information($"Port number is set to {this.information.ConsoleOptions.NetsphereOptions.Port}");
            }
        }

        // Terminals
        this.Terminal.Initialize(false, this.@private.NodePrivateEcdh);
        if (this.Alternative != null)
        {
            this.Alternative.Initialize(true, NodeKey.FromPrivateKey(NodePrivateKey.AlternativePrivateKey)!);
            this.Alternative.Port = NodeAddress.Alternative.Port;
        }

        // Machines
        this.bigMachine.TryCreate<Machines.EssentialNetMachine.Interface>(Identifier.Zero);
    }

    public void Start(Message.Start message)
    {
    }

    public void SetServerTerminalDelegate(CreateServerTerminalDelegate @delegate)
    {
        this.createServerTerminalDelegate = @delegate;
    }

    public MyStatus MyStatus { get; } = new();

    public NetStatus NetStatus { get; }

    public Terminal Terminal { get; }

    public EssentialNode EssentialNode { get; }

    internal Terminal? Alternative { get; }

    private BigMachine<Identifier> bigMachine;

    private Information information;

    private Private @private;

    private CreateServerTerminalDelegate? createServerTerminalDelegate;
}
