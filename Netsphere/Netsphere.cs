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
    public Netsphere(BigMachine<Identifier> bigMachine, Information information, Node node)
    {
        this.bigMachine = bigMachine; // Warning: Can't call BigMachine.TryCreate() in a constructor.
        this.information = information;
        this.Node = node;

        Radio.Open<Message.Configure>(this.Configure);
    }

    public void Configure(Message.Configure message)
    {
        return;
    }

    public void Configure()
    {
        // Set port number
        if (this.information.ConsoleOptions.NetsphereOptions.Port < Constants.MinPort ||
            this.information.ConsoleOptions.NetsphereOptions.Port > Constants.MaxPort)
        {
            var showWarning = false;
            if (this.information.ConsoleOptions.NetsphereOptions.Port != 0)
            {
                showWarning = true;
            }

            this.information.ConsoleOptions.NetsphereOptions.Port = Random.Pseudo.NextInt(Constants.MinPort, Constants.MaxPort + 1);
            if (showWarning)
            {
                Log.Warning($"Port number must be between {Constants.MinPort} and {Constants.MaxPort}");
                Log.Information($"Port number is set to {this.information.ConsoleOptions.NetsphereOptions.Port}");
            }
        }

        // Machines
        this.bigMachine.TryCreate<Machines.NetsphereMachine.Interface>(Identifier.Zero);
    }

    public void Start(ThreadCoreBase parent)
    {
    }

    public MyStatus MyStatus { get; } = new();

    public NetStatus NetStatus { get; } = new();

    public Node Node { get; }

    private BigMachine<Identifier> bigMachine;

    private Information information;
}
