// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData;
using Netsphere.State;

namespace Netsphere.Machines;

[MachineObject(UseServiceProvider = true)]
public partial class NetStatMachine : Machine
{
    public NetStatMachine(ILogger<NetStatMachine> logger, LPBase lpBase, NetControl netControl, NetStat netStat)
    {
        this.logger = logger;
        this.lpBase = lpBase;
        this.netControl = netControl;
        this.netStat = netStat;

        // this.DefaultTimeout = TimeSpan.FromSeconds(5);
    }

    [StateMethod(0)]
    protected async Task<StateResult> Unknown(StateParameter parameter)
    {
        return StateResult.Terminate;
    }

    private readonly ILogger? logger;
    private readonly LPBase lpBase;
    private readonly NetControl netControl;
    private readonly NetStat netStat;
}
