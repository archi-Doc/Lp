// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("inspect")]
public class InspectSubcommand : ISimpleCommand
{
    private readonly LpUnit lpUnit;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly ILogger logger;

    public InspectSubcommand(LpUnit lpUnit, IUserInterfaceService userInterfaceService, ILogger<InspectSubcommand> logger)
    {
        this.lpUnit = lpUnit;
        this.userInterfaceService = userInterfaceService;
        this.logger = logger;
    }

    public void Run(string[] args)
    {
        this.logger.GetWriter(LogLevel.Warning)?.Write("InspectSubcommand");

        this.InspectVersion();
        this.InspectNet();
        this.InspectMerger();
        this.InspectRelayMerger();
        this.InspectLinker();
    }

    public void InspectVersion()
    {
        this.userInterfaceService.WriteLine($"Lp ({Arc.VersionHelper.VersionString})");
    }

    public void InspectNet()
    {
        var options = this.lpUnit.LpBase.Options;
        this.userInterfaceService.WriteLine($"NodeName:{this.lpUnit.LpBase.NodeName}, Alternative:{options.EnableAlternative}, Test:{options.TestFeatures}");

        var netStats = this.lpUnit.NetUnit.NetStats;
        this.userInterfaceService.WriteLine($"Own NetNode ({netStats.GetOwnNodeType().ToString()}): {netStats.GetOwnNetNode().ToString()}");
        this.userInterfaceService.WriteLine($"Remote key: {this.lpUnit.LpBase.RemotePublicKey.ToString()}");
    }

    public void InspectMerger()
    {
        var merger = this.lpUnit.Merger;
        this.userInterfaceService.WriteLine($"Merger public key: {merger.PublicKey}");
    }

    public void InspectRelayMerger()
    {
        var merger = this.lpUnit.RelayMerger;
        this.userInterfaceService.WriteLine($"RelayMerger public key: {merger.PublicKey}");
    }

    public void InspectLinker()
    {
        var merger = this.lpUnit.Linker;
        this.userInterfaceService.WriteLine($"Linker public key: {merger.PublicKey}");
    }
}
