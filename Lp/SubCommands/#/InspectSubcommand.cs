// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("inspect")]
public class InspectSubcommand : ISimpleCommand
{
    private readonly LpUnit lpUnit;
    private readonly IUserInterfaceService userInterfaceService;

    public InspectSubcommand(LpUnit lpUnit, IUserInterfaceService userInterfaceService)
    {
        this.lpUnit = lpUnit;
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
        var options = this.lpUnit.LpBase.Options;
        this.userInterfaceService.WriteLine($"NodeName:{this.lpUnit.LpBase.NodeName}, Alternative:{options.EnableAlternative}, Test:{options.TestFeatures}");

        var netStats = this.lpUnit.NetUnit.NetStats;
        this.userInterfaceService.WriteLine($"Own NetNode ({netStats.GetOwnNodeType().ToString()}): {netStats.GetOwnNetNode().ToString()}");
        this.userInterfaceService.WriteLine($"Remote key: {this.lpUnit.LpBase.RemotePublicKey.ToString()}");
    }

    private void InspectMerger()
    {
        var merger = this.lpUnit.Merger;
        this.userInterfaceService.WriteLine($"Merger public key: {merger.PublicKey}");
    }
}
