// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace LP.Machines;

/// <summary>
/// Check essential nodes and determine MyStatus.ConnectionType.<br/>
/// 1: Connect and get nodes.<br/>
/// 2: Determine MyStatus.ConnectionType.<br/>
/// 3: Check essential nodes.
/// </summary>
[MachineObject(0x4792ab0f, Group = typeof(SingleGroup<>))]
public partial class EssentialNetMachine : Machine<Identifier>
{
    public EssentialNetMachine(BigMachine<Identifier> bigMachine, LPBase lpBase, NetBase netBase, NetControl netControl)
        : base(bigMachine)
    {
        this.NetBase = netBase;
        this.NetControl = netControl;
        this.LPBase = lpBase;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    public LPBase LPBase { get; }

    public NetBase NetBase { get; }

    public NetControl NetControl { get; }

    public bool EnableLogger => this.LPBase.Settings.Flags.LogEssentialNetMachine;

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        this.count++;

        if (this.NetControl.EssentialNode.GetUncheckedNode(out var nodeAddress))
        {
            // var alt = NodeInformation.Alternative;
            // this.Netsphere.EssentialNode.Report(nodeAddress, NodeConnectionResult.Success);

            // nodeAddress = NodeAddress.Alternative;
            using (var terminal = this.NetControl.Terminal.Create(nodeAddress))
            {
                // await terminal.EncryptConnectionAsync();
                var result = await terminal.SendPacketAndReceiveAsync<PacketPunch, PacketPunchResponse>(new PacketPunch(null));
                if (result.Result == NetResult.Success && result.Value is { } value)
                {// Success
                    if (this.EnableLogger)
                    {
                        Logger.Default.Information(Time.GetUtcNow().ToString());
                        Logger.Default.Information($"{this.count} - {value.Endpoint} - {new DateTime(value.UtcMics)}");
                    }

                    this.NetControl.EssentialNode.Report(nodeAddress, NodeConnectionResult.Success);
                }
                else
                {
                    if (this.EnableLogger)
                    {
                        Logger.Default.Information($"Receive timeout: {nodeAddress}");
                    }

                    this.NetControl.EssentialNode.Report(nodeAddress, NodeConnectionResult.Failure);
                }

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

    [StateMethod(1)]
    protected async Task<StateResult> First(StateParameter parameter)
    {
        return StateResult.Terminate;
    }

    private int count = 1;
}
