// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Machines;

/*
/// <summary>
/// Check essential nodes and determine MyStatus.ConnectionType.<br/>
/// 1: Connect and get nodes.<br/>
/// 2: Determine MyStatus.ConnectionType.<br/>
/// 3: Check essential nodes.
/// </summary>
[MachineObject(UseServiceProvider = true)]
public partial class EssentialNetMachine : Machine
{
    public EssentialNetMachine(ILogger<EssentialNetMachine> logger, LPBase lpBase, NetBase netBase, NetControl netControl)
        : base()
    {
        this.logger = logger;
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
        this.logger.TryGet(LogLevel.Information)?.Log($"Essential net machine {this.count}");
        this.count++;

        if (this.NetControl.EssentialNode.GetUncheckedNode(out var nodeAddress))
        {
            // var alt = NodeInformation.Alternative;
            // this.Netsphere.EssentialNode.Report(nodeAddress, NodeConnectionResult.Success);

            // nodeAddress = NodeAddress.Alternative;
            using (var terminal = this.NetControl.Terminal.Create(nodeAddress))
            {
                // await terminal.EncryptConnectionAsync().ConfigureAwait(false);
                var result = await terminal.SendPacketAndReceiveAsync<PacketPunch, PacketPunchResponse>(new PacketPunch(null)).ConfigureAwait(false);
                if (result.Result == NetResult.Success && result.Value is { } value)
                {// Success
                    if (this.EnableLogger)
                    {
                        this.logger.TryGet()?.Log(Time.GetUtcNow().ToString());
                        this.logger.TryGet()?.Log($"{nodeAddress.ToString()} - {value.Endpoint} - {Mics.ToString(value.UtcMics)}");
                    }

                    this.NetControl.EssentialNode.Report(nodeAddress, NodeConnectionResult.Success);
                }
                else
                {
                    if (this.EnableLogger)
                    {
                        this.logger.TryGet()?.Log($"Receive timeout: {nodeAddress}");
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

    private ILogger<EssentialNetMachine> logger;
    private int count = 1;
}*/
