// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using BigMachines;
using Docker.DotNet;
using Docker.DotNet.Models;
using LP;
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

    public RunnerMachine(ILogger<RunnerMachine> logger, BigMachine<Identifier> bigMachine, LPBase lPBase, NetControl netControl)
        : base(bigMachine)
    {
        this.logger = logger;
        this.lpBase = lPBase;
        this.netControl = netControl;

        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        var information = await this.LoadInformation(Path.Combine(this.lpBase.RootDirectory, RunnerInformation.Path));
        if (information == null)
        {// Could not load RunnerInformation.
            return StateResult.Terminate;
        }

        this.Information = information;

        var text = $"127.0.0.1:{this.Information.TargetPort}";
        NodeAddress.TryParse(text, out var nodeAddress);
        this.NodeAddress = nodeAddress;

        this.logger.TryGet()?.Log($"Runner start");
        this.logger.TryGet()?.Log($"Root directory: {this.lpBase.RootDirectory}");
        this.logger.TryGet()?.Log($"{this.Information.ToString()}");
        this.logger.TryGet()?.Log("Press Ctrl+C to exit.");
        await Console.Out.WriteLineAsync();

        this.ChangeState(State.Check, true);
        return StateResult.Continue;
    }

    [StateMethod(1)]
    protected async Task<StateResult> Check(StateParameter parameter)
    {
        this.logger.TryGet()?.Log("Check");

        var result = await RunnerHelper.ExecuteCommand(this.logger, string.Empty);
        if (result == null)
        {
            return StateResult.Terminate;
        }

        var client = new DockerClientConfiguration().CreateClient();
        try
        {
            var containers = await client.Containers.ListContainersAsync(new() { Limit = 10, });
            this.logger.TryGet()?.Log($"Docker: {containers.Count}");

            var progress = new Progress<JSONMessage>();
            await client.Images.CreateImageAsync(
                new ImagesCreateParameters
                {
                    FromImage = "archidoc422/lpconsole",
                    Tag = "latest",
                }, null, progress);
            var containerResponse = await client.Containers.CreateContainerAsync(new()
            {
                Image = "archidoc422/lpconsole",
            });
        }
        catch
        {
            this.logger.TryGet(LogLevel.Fatal)?.Log($"No docker");
        }

        this.logger.TryGet()?.Log($"Result: {result}");
        return StateResult.Terminate;
        return StateResult.Continue;
    }

    public RunnerInformation? Information { get; private set; }

    public NodeAddress? NodeAddress { get; private set; }

    private async Task<RunnerInformation?> LoadInformation(string path)
    {
        try
        {
            var utf8 = await File.ReadAllBytesAsync(path);
            var information = TinyhandSerializer.DeserializeFromUtf8<RunnerInformation>(utf8);
            if (information != null)
            {
                return information;
            }
        }
        catch
        {
        }

        await File.WriteAllBytesAsync(path, TinyhandSerializer.SerializeToUtf8(RunnerInformation.Create()));

        this.logger.TryGet(LogLevel.Error)?.Log($"'{path}' could not be found and was created.");
        this.logger.TryGet(LogLevel.Error)?.Log($"Modify '{RunnerInformation.Path}', and restart LPRunner.");

        return null;
    }

    private ILogger<RunnerMachine> logger;
    private LPBase lpBase;
    private NetControl netControl;
}
