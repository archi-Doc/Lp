// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace LP.Machines;

/// <summary>
/// Check essential nodes and determine MyStatus.ConnectionType.<br/>
/// 1: Connect and get nodes.<br/>
/// 2: Determine MyStatus.ConnectionType.<br/>
/// 3: Check essential nodes.
/// </summary>
[MachineObject(0x4792ab0f, Group = typeof(MachineSingle<>))]
public partial class EssentialNetMachine : Machine<Identifier>
{
    public EssentialNetMachine(BigMachine<Identifier> bigMachine, NetBase netBase, NetControl netControl)
        : base(bigMachine)
    {
        this.NetBase = netBase;
        this.NetControl = netControl;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    public NetBase NetBase { get; }

    public NetControl NetControl { get; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        this.count++;

        if (this.NetControl.EssentialNode.GetUncheckedNode(out var nodeAddress))
        {
            // var alt = NodeInformation.Alternative;
            // this.Netsphere.EssentialNode.Report(nodeAddress, NodeConnectionResult.Success);

            // nodeAddress = NodeAddress.Alternative;
            using (var terminal = this.NetControl.Terminal.Create(nodeAddress))
            {
                /*terminal.SendRaw(new RawPacketPunch(null));
                var data = terminal.ReceiveRaw<PacketPunchResponse>(1000);
                if (data != null)
                {
                    Logger.Default.Information(Time.GetUtcNow().ToString());
                    Logger.Default.Information($"{this.count} - {data.Endpoint} - {new DateTime(data.UtcTicks)}");
                    this.Netsphere.EssentialNode.Report(nodeAddress, NodeConnectionResult.Success);
                }
                else
                {
                    Logger.Default.Information($"Receive timeout: {nodeAddress}");
                    this.Netsphere.EssentialNode.Report(nodeAddress, NodeConnectionResult.Failure);
                }*/

                // this.count <<= 1;
                // this.SetTimeout(TimeSpan.FromSeconds(this.count));
                return StateResult.Continue;
            }
        }

        if (this.NetControl.MyStatus.Type == MyStatus.ConnectionType.Unknown)
        {
        }

        return StateResult.Continue;
    }

    private int count = 1;
}
