// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.NetServices;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("remotebench")]
internal class RemoteBenchSubcommand : ISimpleCommandAsync<RemoteBenchOptions>
{
    public RemoteBenchSubcommand(ILogger<RemoteBenchSubcommand> logger, IUserInterfaceService userInterfaceService, RemoteBenchControl remoteBenchBroker)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.remoteBenchBroker = remoteBenchBroker;
    }

    public async Task RunAsync(RemoteBenchOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"RemoteBench");
        this.remoteBenchBroker.Start(options);
    }

    private RemoteBenchControl remoteBenchBroker;
    private ILogger logger;
    private IUserInterfaceService userInterfaceService;
}

public record RemoteBenchOptions
{
    [SimpleOption("total", Description = "Total")]
    public int Total { get; init; } = 10_000;

    [SimpleOption("concurrent", Description = "Concurrent")]
    public int Concurrent { get; init; } = 25;

    [SimpleOption("netnode", Description = "Node address", Required = false)]
    public string NetNode { get; init; } = string.Empty;

    [SimpleOption("remoteprivatekey", Description = "Remote private key", Required = false)]
    public string RemotePrivateKey { get; init; } = string.Empty;
}
