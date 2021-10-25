// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Net;

namespace LP.Machines;

/// <summary>
/// Check essential nodes and determine MyStatus.ConnectionType.
/// </summary>
[MachineObject(0x4792ab0f, Group = typeof(MachineSingle<>))]
public partial class EssentialNetMachine : Machine<Identifier>
{
    public EssentialNetMachine(BigMachine<Identifier> bigMachine, Netsphere netsphere)
        : base(bigMachine)
    {
        this.Netsphere = netsphere;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    public Netsphere Netsphere { get; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        if (this.Netsphere.MyStatus.Type == MyStatus.ConnectionType.Unknown)
        {
            if (this.Netsphere.Node.GetUncheckedNode(out var nodeAddress))
            {
                this.Netsphere.Node.Report(nodeAddress, NodeConnectionResult.Success);

                /*using (var terminal = this.Netsphere.NetTerminal.Create(nodeAddress, this.BigMachine.Core))
                {
                    terminal.Send(Punch);
                    terminal.Receive();
                }*/
            }
        }

        return StateResult.Continue;
    }
}
