// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("inspect")]
public class InspectSubcommand : ISimpleCommand
{
    private readonly Control control;
    private readonly IUserInterfaceService userInterfaceService;

    public InspectSubcommand(Control control, IUserInterfaceService userInterfaceService)
    {
        this.control = control;
        this.userInterfaceService = userInterfaceService;
    }

    public void Run(string[] args)
    {
        this.InspectVersion();
        this.InspectNet();
        this.InspectMerger();
    }

    private void InspectVersion()
    {
        this.userInterfaceService.WriteLine($"Lp ({Netsphere.Version.VersionHelper.VersionString})");
    }

    private void InspectNet()
    {
        var options = this.control.LpBase.Options;
        this.userInterfaceService.WriteLine($"NodeName:{this.control.LpBase.NodeName}, Alternative:{options.EnableAlternative}, Test:{options.TestFeatures}");

        var netStats = this.control.NetUnit.NetStats;
        this.userInterfaceService.WriteLine($"Own NetNode ({netStats.GetOwnNodeType().ToString()}): {netStats.GetOwnNetNode().ToString()}");
    }

    private void InspectMerger()
    {
        var merger = this.control.Merger;
        this.userInterfaceService.WriteLine($"Merger public key: {merger.PublicKey}");
    }
}
