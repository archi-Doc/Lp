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
        var ni = NodeInformation.Alternative;
        var ta = this.Netsphere.Terminal.Create(ni);
        var pp = new PacketPunch(null, DateTime.UtcNow.Ticks);
        ta.Send(pp);

        if (this.Netsphere.EssentialNode.GetUncheckedNode(out var nodeAddress))
        {
            // var alt = NodeInformation.Alternative;
            // this.Netsphere.EssentialNode.Report(nodeAddress, NodeConnectionResult.Success);

            // nodeAddress = NodeAddress.Alternative;
            using (var terminal = this.Netsphere.Terminal.Create(nodeAddress))
            {
                terminal.SendUnmanaged_Punch();
                this.BigMachine.Core.Sleep(100);
                var data = terminal.Receive<PacketPunchResponse>(1000);
                if (data != null)
                {
                    Logger.Default.Information(DateTime.UtcNow.ToString());
                    Logger.Default.Information($"{this.count} - {data.Endpoint} - {new DateTime(data.UtcTicks)}");
                    this.Netsphere.EssentialNode.Report(nodeAddress, NodeConnectionResult.Success);
                }
                else
                {
                    Logger.Default.Information($"Receive timeout: {nodeAddress}");
                    this.Netsphere.EssentialNode.Report(nodeAddress, NodeConnectionResult.Failure);
                }

                // this.count <<= 1;
                // this.SetTimeout(TimeSpan.FromSeconds(this.count));
                return StateResult.Continue;
            }
        }

        if (this.Netsphere.MyStatus.Type == MyStatus.ConnectionType.Unknown)
        {
        }

        return StateResult.Continue;
    }

    private int count = 1;
}
