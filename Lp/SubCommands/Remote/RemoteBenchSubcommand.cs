// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.NetServices;
using Netsphere.Misc;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("remotebench")]
internal class RemoteBenchSubcommand : ISimpleCommand<RemoteBenchOptions>
{
    public RemoteBenchSubcommand(ILogger<RemoteBenchSubcommand> logger, IUserInterfaceService userInterfaceService, RemoteBenchControl remoteBenchBroker, NtpCorrection ntpCorrection)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.remoteBenchBroker = remoteBenchBroker;
        this.ntpCorrection = ntpCorrection;
    }

    public async Task Execute(RemoteBenchOptions options, string[] args, CancellationToken cancellationToken)
    {
        await this.ntpCorrection.CorrectMicsAndUnitLogger(cancellationToken: cancellationToken);

        this.logger.GetWriter()?.Write($"RemoteBench");
        if (this.remoteBenchBroker.Start(options, cancellationToken) is { } task)
        {// Wait for the aggregation, since the cancellation token is canceled when this command returns.
            await task.ConfigureAwait(false);
        }
    }

    private readonly RemoteBenchControl remoteBenchBroker;
    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly NtpCorrection ntpCorrection;
}

public record RemoteBenchOptions
{
    [SimpleOption("Total", Description = "Total")]
    public int Total { get; init; } = 10_000;

    [SimpleOption("Concurrent", Description = "Concurrent")]
    public int Concurrent { get; init; } = 25;

    [SimpleOption("Node", Description = "Node address", IsRequired = false)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("RemotePrivatekey", Description = "Remote private key", IsRequired = false)]
    public string RemotePrivateKey { get; init; } = string.Empty;
}
