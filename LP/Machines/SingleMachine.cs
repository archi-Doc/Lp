// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Services;

namespace LP.Machines;

[MachineObject(0x0f0d509d, Group = typeof(SingleGroup<>))]
// [TinyhandObject(UseServiceProvider = true)]
public partial class SingleMachine : Machine<Identifier>
{
    public SingleMachine(BigMachine<Identifier> bigMachine, IConsoleService consoleService)
        : base(bigMachine)
    {
        this.consoleService = consoleService;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        // this.consoleService.WriteLine($"Single: ({this.Identifier.ToString()}) - {this.Count++}");

        return StateResult.Continue;
    }

    private IConsoleService consoleService;
}
