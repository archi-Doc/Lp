// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.NetServices;
using Netsphere.Misc;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("remotebench")]
internal class RemoteBenchSubcommand : ISimpleCommandAsync<RemoteBenchOptions>
{
    public RemoteBenchSubcommand(ILogger<RemoteBenchSubcommand> logger, IUserInterfaceService userInterfaceService, RemoteBenchControl remoteBenchBroker, NtpCorrection ntpCorrection)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.remoteBenchBroker = remoteBenchBroker;
        this.ntpCorrection = ntpCorrection;
    }

    public async Task RunAsync(RemoteBenchOptions options, string[] args)
    {
        await this.ntpCorrection.CorrectMicsAndUnitLogger();

        this.logger.TryGet()?.Log($"RemoteBench");
        this.remoteBenchBroker.Start(options);
    }

    private readonly RemoteBenchControl remoteBenchBroker;
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NtpCorrection ntpCorrection;
}

public record RemoteBenchOptions
{
    [SimpleOption("total", Description = "Total")]
    public int Total { get; init; } = 10_000;

    [SimpleOption("concurrent", Description = "Concurrent")]
    public int Concurrent { get; init; } = 25;

    [SimpleOption("node", Description = "Node address", Required = false)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("remoteprivatekey", Description = "Remote private key", Required = false)]
    public string RemotePrivateKey { get; init; } = string.Empty;
}
