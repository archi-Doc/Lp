// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using SimpleCommandLine;

namespace RemoteDataServer;

[SimpleCommand("default", Default = true)]
public class DefaultCommand : ISimpleCommandAsync<DefaultCommandOptions>
{
    public DefaultCommand(ILogger<DefaultCommandOptions> logger, NetControl netControl)
    {
        this.logger = logger;
        this.netControl = netControl;
    }

    public async Task RunAsync(DefaultCommandOptions options, string[] args)
    {
        if (NodePrivateKey.TryParse(options.NodePrivateKey, out var privateKey))
        {
            this.netControl.NetBase.SetNodePrivateKey(privateKey);
        }
        else if (CryptoHelper.TryParseFromEnvironmentVariable<NodePrivateKey>(NetConstants.NodePrivateKeyName, out privateKey))
        {
            this.netControl.NetBase.SetNodePrivateKey(privateKey);
        }

        await ThreadCore.Root.Delay(Timeout.InfiniteTimeSpan); // Wait until the server shuts down.
    }

    private readonly NetControl netControl;
    private readonly ILogger logger;
}

public record DefaultCommandOptions
{
    [SimpleOption("directory", Description = "Directory")]
    public string Directory { get; init; } = "Data";

    [SimpleOption("nodeprivatekey", Description = "Node private key")]
    public string NodePrivateKey { get; init; } = string.Empty;
}
