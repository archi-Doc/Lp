// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Arc.Threading;
using Arc.Unit;
using LP;
using LP.Data;
using LP.NetServices;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;
using Tinyhand;

namespace LPRunner;

public class Runner
{
    public enum RunnerState
    {
        Check,
        Start,
        Running,
        Restarting,
        Abort,
    }

    public Runner(ILogger<Runner> logger, LPBase lPBase, NetControl netControl)
    {
        this.logger = logger;
        this.lpBase = lPBase;
        this.netControl = netControl;
    }

    public async Task Run()
    {
        var information = await this.LoadInformation(Path.Combine(this.lpBase.RootDirectory, RunnerInformation.Path));
        if (information == null)
        {
            return;
        }

        this.Information = information;

        var text = $"127.0.0.1:{this.Information.TargetPort}";
        NodeAddress.TryParse(text, out var nodeAddress);
        if (nodeAddress == null)
        {
            return;
        }

        this.NodeAddress = nodeAddress;

        this.logger.TryGet()?.Log($"Runner start");
        this.logger.TryGet()?.Log($"Root directory: {this.lpBase.RootDirectory}");
        this.logger.TryGet()?.Log($"{this.Information.ToString()}");
        this.logger.TryGet()?.Log("Press Ctrl+C to exit.");
        await Console.Out.WriteLineAsync();

        while (!ThreadCore.Root.IsTerminated)
        {
            try
            {
                await this.Process();
                if (this.State == RunnerState.Abort)
                {
                    break;
                }
            }
            catch
            {
                break;
            }

            ThreadCore.Root.Sleep(1000);
        }

        /*var process = new Process();
        process.StartInfo.FileName = "/bin/bash";
        process.StartInfo.ArgumentList = "ls";*/

        try
        {
            /*var process = Process.Start("/bin/bash", "ls");
            process.WaitForExit();*/

            var startInfo = new ProcessStartInfo
            {
                FileName = @"/bin/bash",
                Arguments = "-c \"echo hello\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                Console.WriteLine(result);
            }
        }
        catch
        {
            this.logger.TryGet()?.Log("ex");
        }

        this.logger.TryGet()?.Log("Runner end");
    }

    public Task Process() => this.State switch
    {
        RunnerState.Check => this.ProcessCheck(),
        RunnerState.Start => this.ProcessStart(),
        _ => Task.CompletedTask,
    };

    public async Task ProcessCheck()
    {
        var result = await this.SendAcknowledge();
        if (result == NetResult.Success)
        {
            this.State = RunnerState.Running;
            return;
        }

        this.State = RunnerState.Start;
    }

    public async Task ProcessStart()
    {
        await this.ExecuteCommand(this.Information.RunCommand);

        await Task.Delay(5000, ThreadCore.Root.CancellationToken);

        var result = await this.SendAcknowledge();
        if (result == NetResult.Success)
        {
            this.State = RunnerState.Running;
            return;
        }

        this.State = RunnerState.Abort;
    }

    public async Task ExecuteCommand(string command)
    {
        this.logger.TryGet()?.Log($"Command: {command}");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = @"/bin/bash",
                Arguments = "-c \"echo hello\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string result = process.StandardOutput.ReadToEnd();
                await process.WaitForExitAsync(ThreadCore.Root.CancellationToken);

                Console.WriteLine(result);
            }
        }
        catch
        {
            this.logger.TryGet()?.Log("ex");
        }
    }

    public async Task<NetResult> SendAcknowledge()
    {
        using (var terminal = this.netControl.Terminal.Create(this.NodeAddress))
        {
            var remoteControl = terminal.GetService<IRemoteControlService>();
            var result = await remoteControl.Acknowledge();
            this.logger.TryGet()?.Log($"Acknowledge: {result}");
            return result;
        }
    }

    public async Task<RunnerInformation?> LoadInformation(string path)
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

    public RunnerState State { get; private set; }

    public RunnerInformation Information { get; private set; } = default!;

    public NodeAddress NodeAddress { get; private set; } = default!;

    private ILogger<Runner> logger;
    private LPBase lpBase;
    private NetControl netControl;
}
