// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Arc.Crypto;
using Arc.Unit;
using LP.NetServices;
using Netsphere.RemoteData;
using SimpleCommandLine;

namespace NetsphereTest;

[SimpleCommand("stream")]
public class StreamTestSubcommand : ISimpleCommandAsync<StreamTestOptions>
{
    public StreamTestSubcommand(ILogger<StreamTestSubcommand> logger, NetTerminal netTerminal)
    {
        this.logger = logger;
        this.netTerminal = netTerminal;
    }

    public async Task RunAsync(StreamTestOptions options, string[] args)
    {
        var data = new byte[10_000_000];
        RandomVault.Pseudo.NextBytes(data);
        var hash = FarmHash.Hash64(data);

        var r = await NetHelper.TryGetStreamService<IRemoteBenchHost>(this.netTerminal, options.NetNode, options.RemotePrivateKey, 100_000_000);
        if (r.Connection is null ||
            r.Service is null)
        {
            return;
        }

        try
        {
            this.logger.TryGet()?.Log($"IRemoteBenchHost.GetHash()");

            var sendStream = await r.Service.GetHash(data.Length);
            if (sendStream is null)
            {
                this.logger.TryGet(LogLevel.Error)?.Log($"No stream");
                return;
            }

            var result = await sendStream.Send(data);
            var result2 = await sendStream.CompleteSendAndReceive();
            if (result2.Result != NetResult.Success)
            {
                this.logger.TryGet(LogLevel.Error)?.Log(result2.Result.ToString());
            }
            else
            {
                this.logger.TryGet(LogLevel.Error)?.Log((result2.Value == hash).ToString());
            }
        }
        finally
        {
            r.Connection.Close();
        }
    }

    private readonly NetTerminal netTerminal;
    private readonly ILogger logger;
}

public record StreamTestOptions
{
    [SimpleOption("netnode", Description = "Node address")]
    public string NetNode { get; init; } = string.Empty;

    [SimpleOption("remoteprivatekey", Description = "Remote private key")]
    public string RemotePrivateKey { get; init; } = string.Empty;
}
