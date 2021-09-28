﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

global using System;
global using System.IO;
global using System.Net;
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
    public Netsphere(BigMachine<Identifier> bigMachine, Information information)
    {
        this.bigMachine = bigMachine; // Warning: Can't call BigMachine.TryCreate() in a constructor.
        this.information = information;

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

        // Nodes
        var nodes = this.information.ConsoleOptions.NetsphereOptions.Nodes;
        nodes = "192.168.0.1:100,, [192.168.0.2]:200";
        foreach (var x in nodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (NodeAddress.TryParse(x, out var address))
            {
                // this.World.AddEssential(address);
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

    private BigMachine<Identifier> bigMachine;

    private Information information;
}
