// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.Machines;

[MachineObject(UseServiceProvider = true)]
// [TinyhandObject(UseServiceProvider = true)]
public partial class LogTesterMachine : Machine
{
    public LogTesterMachine(ILogger<LogTesterMachine> logger)
    {
        this.logger = logger;
        this.DefaultTimeout = TimeSpan.FromSeconds(2);
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        this.logger.TryGet(LogLevel.Information)?.Log($"Log test: {this.Count++}");
        // this.logger.TryGet()?.Log($"{DateTime.Now.ToString()}, {Mics.ToDateTime(Mics.GetFixedUtcNow()).ToString()}");

        return StateResult.Continue;
    }

    private ILogger<LogTesterMachine> logger;
}
