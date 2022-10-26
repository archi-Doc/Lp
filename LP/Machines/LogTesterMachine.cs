// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Machines;

[MachineObject(0xa91505d4, Group = typeof(SingleGroup<>))]
// [TinyhandObject(UseServiceProvider = true)]
public partial class LogTesterMachine : Machine<Identifier>
{
    public LogTesterMachine(BigMachine<Identifier> bigMachine, Control control, ILogger<LogTesterMachine> logger)
        : base(bigMachine)
    {
        this.control = control;
        this.logger = logger;
        this.DefaultTimeout = TimeSpan.FromSeconds(2);
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        this.logger.TryGet(LogLevel.Information)?.Log($"Log test2: {this.Count++}");
        // this.logger.TryGet()?.Log($"{DateTime.Now.ToString()}, {Mics.ToDateTime(Mics.GetFixedUtcNow()).ToString()}");

        return StateResult.Continue;
    }

    private Control control;
    private ILogger<LogTesterMachine> logger;
}
