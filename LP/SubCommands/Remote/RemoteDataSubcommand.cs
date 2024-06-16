// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Logging;
using Microsoft.Extensions.DependencyInjection;
using Netsphere.Interfaces;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("remotedata")]
internal class RemoteDataSubcommand : ISimpleCommandAsync<RemoteDataOptions>
{
    public RemoteDataSubcommand(IServiceProvider serviceProvider, ILogger<RemoteDataOptions> logger, NetTerminal netTerminal)
    {
        this.logger = logger;
        this.fileLogger = serviceProvider.GetService<FileLogger<NetsphereLoggerOptions>>();
        this.netTerminal = netTerminal;
    }

    public async Task RunAsync(RemoteDataOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"RemoteData");

        if (this.fileLogger is not null)
        {// Reset
            this.fileLogger.DeleteAllLogs();
        }

        var r = await NetHelper.TryGetStreamService<IRemoteData>(this.netTerminal, options.Node, options.RemotePrivateKey, 100_000_000);
        if (r.Connection is null ||
            r.Service is null)
        {
            return;
        }

        try
        {
            this.logger.TryGet()?.Log($"Put RemoteData.data");
            var sendStream = await r.Service.Put("RemoteData.data", 1024 * 1024);
            if (sendStream is null)
            {
                return;
            }

            await sendStream.Send(new byte[1024 * 2024]);
            var result = await sendStream.CompleteSendAndReceive();
            this.logger.TryGet()?.Log($"Put({result}) RemoteData.data {sendStream.SentLength} bytes");

            if (this.fileLogger is null)
            {
                return;
            }

            await this.fileLogger.Flush(false);

            var path = this.fileLogger.GetCurrentPath();
            using var fileStream = File.OpenRead(path);
            sendStream = await r.Service.Put("RemoteData.txt", fileStream.Length);
            if (sendStream is not null)
            {
                var r3 = await NetHelper.StreamToSendStream(fileStream, sendStream);
            }
        }
        catch
        {
        }
        finally
        {
            r.Connection.Dispose();
        }
    }

    private readonly ILogger logger;
    private readonly IFileLogger? fileLogger;
    private readonly NetTerminal netTerminal;
}

public record RemoteDataOptions
{
    [SimpleOption("node", Description = "Node address", Required = false)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("remoteprivatekey", Description = "Remote private key", Required = false)]
    public string RemotePrivateKey { get; init; } = string.Empty;
}
