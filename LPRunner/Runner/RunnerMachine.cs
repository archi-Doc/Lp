// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using BigMachines;
using Docker.DotNet;
using Docker.DotNet.Models;
using LP;
using LP.NetServices;
using Netsphere;
using Tinyhand;

namespace LPRunner;

[MachineObject(0x0b5190d7, Group = typeof(SingleGroup<Identifier>))]
public partial class RunnerMachine : Machine<Identifier>
{
    public enum LPStatus
    {
        NotRunning,
        Container,
        Running,
    }

    public RunnerMachine(ILogger<RunnerMachine> logger, BigMachine<Identifier> bigMachine, LPBase lPBase, NetControl netControl, RunnerInformation information)
        : base(bigMachine)
    {
        this.logger = logger;
        this.lpBase = lPBase;
        this.netControl = netControl;
        this.Information = information;

        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        var text = $"127.0.0.1:{this.Information.DestinationPort}";
        NodeAddress.TryParse(text, out var nodeAddress);
        this.NodeAddress = nodeAddress;

        this.docker = DockerRunner.Create(this.logger, this.Information);
        if (this.docker == null)
        {
            this.logger.TryGet(LogLevel.Fatal)?.Log($"No docker");
            return StateResult.Terminate;
        }

        this.logger.TryGet()?.Log($"Runner start");
        this.logger.TryGet()?.Log($"Root directory: {this.lpBase.RootDirectory}");
        this.logger.TryGet()?.Log($"{this.Information.ToString()}");
        this.logger.TryGet()?.Log("Press Ctrl+C to exit.");
        await Console.Out.WriteLineAsync();

        // Delete container
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
        var status = await this.GetLPStatus();
        this.logger.TryGet()?.Log($"Status: {status}");

        if (status == LPStatus.Running)
        {// Running
            this.checkRetry = 0;
            this.ChangeState(State.Running);
            return StateResult.Continue;
        }
        else if (status == LPStatus.NotRunning)
        {// Run container
            if (await this.docker.RunContainer() == false)
            {
                return StateResult.Terminate;
            }

            this.SetTimeout(TimeSpan.FromSeconds(10));
            return StateResult.Continue;
        }
        else
        {// Container
            await this.docker.RestartContainer();
            this.SetTimeout(TimeSpan.FromSeconds(10));
            return StateResult.Continue;
        }
    }

    [StateMethod(3)]
    protected async Task<StateResult> Running(StateParameter parameter)
    {
        var result = await this.SendAcknowledge();
        this.logger.TryGet()?.Log($"Running: {result}");
        if (result != NetResult.Success)
        {
            this.ChangeState(State.Check);
        }

        this.SetTimeout(TimeSpan.FromSeconds(10));
        return StateResult.Continue;
    }

    public RunnerInformation Information { get; private set; }

    public NodeAddress? NodeAddress { get; private set; }

    private async Task<LPStatus> GetLPStatus()
    {
        if (await this.SendAcknowledge() == NetResult.Success)
        {
            return LPStatus.Running;
        }

        if (this.docker == null)
        {
            return LPStatus.NotRunning;
        }

        var containers = await this.docker.EnumerateContainersAsync();
        if (containers.Count() > 0)
        {
            return LPStatus.Container;
        }

        return LPStatus.NotRunning;
    }

    private async Task<NetResult> SendAcknowledge()
    {
        if (this.NodeAddress == null)
        {
            return NetResult.NoNodeInformation;
        }

        using (var terminal = this.netControl.Terminal.Create(this.NodeAddress))
        {
            var remoteControl = terminal.GetService<IRemoteControlService>();
            var result = await remoteControl.Acknowledge();
            this.logger.TryGet()?.Log($"Acknowledge: {result}");
            return result;
        }
    }

    private ILogger<RunnerMachine> logger;
    private LPBase lpBase;
    private NetControl netControl;
    private DockerRunner? docker;
    private int checkRetry;
}
