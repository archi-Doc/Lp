// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

    public RunnerMachine(ILogger<RunnerMachine> logger, NetControl netControl, RunnerInformation information)
    {
        this.logger = logger;
        this.netControl = netControl;
        this.information = information;

        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        this.docker = await DockerRunner.Create(this.logger, this.information);
        if (this.docker == null)
        {
            this.logger.TryGet(LogLevel.Fatal)?.Log($"Docker is not available");
            return StateResult.Terminate;
        }

        this.logger.TryGet()?.Log($"Runner start");
        // this.logger.TryGet()?.Log($"Root directory: {this.lpBase.RootDirectory}");
        // var nodeInformation = this.netControl.NetStatus.GetMyNodeInformation(false);
        // this.logger.TryGet()?.Log($"Port: {nodeInformation.Port}, Public key: ({nodeInformation.PublicKey.ToString()})");
        this.logger.TryGet()?.Log($"{this.information.ToString()}");
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
        var address = this.information.TryGetDualAddress();
        if (!address.IsValid)
        {
            return NetResult.NoNodeInformation;
        }

        var node = await this.netControl.NetTerminal.UnsafeGetNetNode(address);
        if (node is null)
        {
            return NetResult.NoNodeInformation;
        }

        using (var terminal = await this.netControl.NetTerminal.Connect(node))
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
    private readonly NetControl netControl;
    private readonly RunnerInformation information;
    private DockerRunner? docker;
    private int checkRetry;
}
