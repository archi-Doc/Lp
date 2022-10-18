// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using BigMachines;
using LP;
using Netsphere;
using SimpleCommandLine;

namespace LPRunner;

[SimpleCommand("run", Default = true)]
public class ConsoleCommand : ISimpleCommandAsync
{
    public ConsoleCommand(ILogger<ConsoleCommand> logger, Runner runner, BigMachine<Identifier> bigMachine, NetControl netControl)
    {
        this.logger = logger;
        this.runner = runner;
        this.bigMachine = bigMachine;
        this.netControl = netControl;
    }

    public async Task RunAsync(string[] args)
    {
        var runner = this.bigMachine.CreateOrGet<RunnerMachine.Interface>(Identifier.Zero);

        this.bigMachine.Start();

        while (!this.bigMachine.Core.IsTerminated)
        {
            if (!this.bigMachine.IsActive())
            {
                break;
            }
            else
            {
                await this.bigMachine.Core.WaitForTerminationAsync(1000);
            }
        }

        // await this.bigMachine.Core.WaitForTerminationAsync(-1);
        // await this.runner.Run();
    }

    private ILogger<ConsoleCommand> logger;
    private Runner runner;
    private BigMachine<Identifier> bigMachine;
    private NetControl netControl;
}
