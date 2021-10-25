// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using System;
global using System.Net;
global using System.Threading.Tasks;
global using Arc.Threading;
global using BigMachines;
global using CrossChannel;
global using LP;
global using LP.Net;
global using Serilog;
global using Tinyhand;
global using ValueLink;

namespace LP.Net;

public class Netsphere
{
    public const int MaxPayload = 1432; // 1432 bytes
    public const int MinPort = 49152; // Ephemeral port 49152 - 60999
    public const int MaxPort = 60999;

    public Netsphere(BigMachine<Identifier> bigMachine, Information information, Node node, Pipe rawPipe)
    {
        this.bigMachine = bigMachine; // Warning: Can't call BigMachine.TryCreate() in a constructor.
        this.information = information;
        this.Node = node;
        this.rawPipe = rawPipe;

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
                Log.Warning($"Port number must be between {Netsphere.MinPort} and {Netsphere.MaxPort}");
                Log.Information($"Port number is set to {this.information.ConsoleOptions.NetsphereOptions.Port}");
            }
        }

        // Machines
        this.bigMachine.TryCreate<Machines.EssentialNetMachine.Interface>(Identifier.Zero);
    }

    public void Start(Message.Start message)
    {
    }

    public MyStatus MyStatus { get; } = new();

    public NetStatus NetStatus { get; } = new();

    public Node Node { get; }

    private BigMachine<Identifier> bigMachine;

    private Information information;

    private Pipe rawPipe;
}
