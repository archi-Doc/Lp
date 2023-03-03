// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using LP.NetServices;
using Netsphere;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("remotebench")]
internal class RemoteBenchSubcommand : ISimpleCommandAsync<RemoteBenchOptions>
{
    public RemoteBenchSubcommand(ILogger<RemoteBenchSubcommand> logger, IUserInterfaceService userInterfaceService, NetControl netControl, RemoteBenchBroker remoteBenchBroker)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.netControl = netControl;
        this.remoteBenchBroker = remoteBenchBroker;
    }

    public async Task RunAsync(RemoteBenchOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"RemoteBench");
        this.remoteBenchBroker.Start(options.Total, options.Concurrent);
    }

    private NetControl netControl;
    private RemoteBenchBroker remoteBenchBroker;
    private ILogger<RemoteBenchSubcommand> logger;
    private IUserInterfaceService userInterfaceService;
}

public record RemoteBenchOptions
{
    [SimpleOption("total", Description = "Total")]
    public int Total { get; init; } = 1_00;

    [SimpleOption("concurrent", Description = "Concurrent")]
    public int Concurrent { get; init; } = 25;
}
