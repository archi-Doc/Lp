// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Net;

namespace LP.Machines;

[MachineObject(0x4792ab0f, Group = typeof(MachineSingle<>))]
public partial class NetsphereMachine : Machine<Identifier>
{
    public NetsphereMachine(BigMachine<Identifier> bigMachine, Netsphere netsphere)
        : base(bigMachine)
    {
        this.Netsphere = netsphere;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    public Netsphere Netsphere { get; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"{Time.LocalTime}");
        Console.WriteLine($"{this.Netsphere.MyStatus.Type}");

        if (this.Netsphere.MyStatus.Type == MyStatus.ConnectionType.Unknown)
        {
        }

        return StateResult.Continue;
    }
}
