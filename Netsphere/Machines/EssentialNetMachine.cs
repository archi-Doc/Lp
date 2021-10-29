// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Net;

namespace LP.Machines;

/// <summary>
/// Check essential nodes and determine MyStatus.ConnectionType.
/// </summary>
[MachineObject(0x4792ab0f, Group = typeof(MachineSingle<>))]
public partial class EssentialNetMachine : Machine<Identifier>
{
    public EssentialNetMachine(BigMachine<Identifier> bigMachine, Information information, Netsphere netsphere)
        : base(bigMachine)
    {
        this.Information = information;
        this.Netsphere = netsphere;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    public Information Information { get; }

    public Netsphere Netsphere { get; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        if (this.Netsphere.EssentialNode.GetUncheckedNode(out var nodeAddress))
        {
            // this.Netsphere.EssentialNode.Report(nodeAddress, NodeConnectionResult.Success);

            nodeAddress = new(IPAddress.Loopback, (ushort)this.Information.ConsoleOptions.NetsphereOptions.Port);
            using (var terminal = this.Netsphere.Terminal.Create(nodeAddress))
            {
                terminal.SendPunch();
                this.BigMachine.Core.Sleep(100);
                var data = terminal.Receive<PacketPunchResponse>(1000);

                return StateResult.Continue;
            }
        }

        if (this.Netsphere.MyStatus.Type == MyStatus.ConnectionType.Unknown)
        {
        }

        return StateResult.Continue;
    }
}
