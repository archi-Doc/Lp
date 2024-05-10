﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using BigMachines;
using Netsphere;
using Netsphere.Packet;

namespace Netsphere.Runner;

[MachineObject(UseServiceProvider = true)]
public partial class RunnerMachine : Machine
{
    public enum Status
    {
        NoContainer,
        Container,
        Running,
    }

    public RunnerMachine(ILogger<RunnerMachine> logger, NetTerminal netTerminal, RunOptions options)
    {
        this.logger = logger;
        this.netTerminal = netTerminal;
        this.options = options;

        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    protected override void OnCreate(object? createParam)
    {
        if (createParam is RunOptions options)
        {
            this.runOptions = options;
        }
    }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        this.docker = await DockerRunner.Create(this.logger, this.options);
        if (this.docker == null)
        {
            this.logger.TryGet(LogLevel.Fatal)?.Log($"Docker is not available");
            return StateResult.Terminate;
        }

        this.logger.TryGet()?.Log($"Runner start");
        // this.logger.TryGet()?.Log($"Root directory: {this.lpBase.RootDirectory}");
        // var nodeInformation = this.netControl.NetStatus.GetMyNodeInformation(false);
        // this.logger.TryGet()?.Log($"Port: {nodeInformation.Port}, Public key: ({nodeInformation.PublicKey.ToString()})");
        this.logger.TryGet()?.Log($"{this.options.ToString()}");
        this.logger.TryGet()?.Log("Press Ctrl+C to exit.");
        await Console.Out.WriteLineAsync();

        // Remove container
        await this.docker.RemoveContainer();

        this.ChangeState(State.Check, true);
        return StateResult.Continue;
    }

    [StateMethod(1)]
    protected async Task<StateResult> Check(StateParameter parameter)
    {
        if (this.docker == null)
        {
            return StateResult.Terminate;
        }

        if (this.checkRetry > 10)
        {
            return StateResult.Terminate;
        }

        this.logger.TryGet()?.Log($"Check ({this.checkRetry++})");
        var status = await this.GetStatus();
        this.logger.TryGet()?.Log($"Status: {status}");

        if (status == Status.Running)
        {// Running
            this.checkRetry = 0;
            this.ChangeState(State.Running);
            return StateResult.Continue;
        }
        else if (status == Status.NoContainer)
        {// No container -> Run
            if (await this.docker.RunContainer() == false)
            {
                return StateResult.Terminate;
            }

            this.TimeUntilRun = TimeSpan.FromSeconds(30);
            return StateResult.Continue;
        }
        else
        {// Container -> Try restart
            await this.docker.RestartContainer();
            this.TimeUntilRun = TimeSpan.FromSeconds(10);
            return StateResult.Continue;
        }
    }

    [StateMethod(3)]
    protected async Task<StateResult> Running(StateParameter parameter)
    {
        /*var result = await this.SendAcknowledge();
        this.logger.TryGet()?.Log($"Running: {result}");
        if (result != NetResult.Success)
        {
            this.ChangeState(State.Check);
        }*/

        this.TimeUntilRun = TimeSpan.FromSeconds(10);
        return StateResult.Continue;
    }

    [CommandMethod]
    protected async Task<CommandResult> Restart()
    {
        this.logger.TryGet()?.Log("RemoteControl -> Restart");

        // Remove container
        if (this.docker != null)
        {
            await this.docker.RemoveContainer();
        }

        this.ChangeState(State.Check);
        return CommandResult.Success;
    }

    private async Task<Status> GetStatus()
    {
        if (await this.SendAcknowledge() == NetResult.Success)
        {
            return Status.Running;
        }

        if (this.docker == null)
        {
            return Status.NoContainer;
        }

        var containers = await this.docker.EnumerateContainersAsync();
        if (containers.Count() > 0)
        {
            return Status.Container;
        }

        return Status.NoContainer;
    }

    private async Task<NetResult> SendAcknowledge()
    {
        var node = await this.options.TryGetContainerNode(this.netTerminal);
        if (node is null)
        {
            return NetResult.NoNodeInformation;
        }

        using (var terminal = await this.netTerminal.Connect(node))
        {
            if (terminal is null)
            {
                return NetResult.NoNetwork;
            }

            var result = await terminal.SendAndReceive<PingPacket, PingPacketResponse>(new());
            this.logger.TryGet()?.Log($"Ping: {result.Result}");
            return result.Result;
        }
    }

    private readonly ILogger logger;
    private readonly NetTerminal netTerminal;
    private readonly RunOptions options;
    private RunOptions runOptions = new();
    private DockerRunner? docker;
    private int checkRetry;
}
