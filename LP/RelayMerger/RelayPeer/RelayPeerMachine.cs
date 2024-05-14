// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Machines;

[MachineObject(UseServiceProvider = true)]
public partial class RelayPeerMachine : Machine
{// Control: context.AddSingleton<Machines.RelayPeerMachine>(), Control.RunMachines()
    public RelayPeerMachine(ILogger<RelayPeerMachine> logger, NetControl netControl)
    {
        this.logger = logger;
        this.netControl = netControl;

        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        this.logger.TryGet()?.Log("Initial");
        return StateResult.Continue;
    }

    private readonly ILogger logger;
    private readonly NetControl netControl;
}
