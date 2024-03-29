// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Logging;
using LP.NetServices;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("remotedata")]
internal class RemoteDataSubcommand : ISimpleCommandAsync<RemoteDataOptions>
{
    public RemoteDataSubcommand(ServiceProvider serviceProvider, ILogger<RemoteDataOptions> logger, IUserInterfaceService userInterfaceService, RemoteBenchControl remoteBenchBroker)
    {
        this.logger = logger;
        this.fileLogger = serviceProvider.GetService<FileLogger<NetsphereLoggerOptions>>();
    }

    public async Task RunAsync(RemoteDataOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"RemoteData");


    }

    private readonly ILogger logger;
    private readonly IFileLogger? fileLogger;
}

public record RemoteDataOptions
{
    [SimpleOption("netnode", Description = "Node address", Required = false)]
    public string NetNode { get; init; } = string.Empty;

    [SimpleOption("remoteprivatekey", Description = "Remote private key", Required = false)]
    public string RemotePrivateKey { get; init; } = string.Empty;
}
