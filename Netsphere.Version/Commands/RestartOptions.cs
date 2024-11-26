// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.Subcommands;

public record RestartOptions
{
    [SimpleOption("RunnerNode", Description = "Runner nodes", Required = true)]
    public string RunnerNode { get; init; } = string.Empty;

    [SimpleOption(NetConstants.RemotePrivateKeyName, Description = "Private key for remote operation", GetEnvironmentVariable = true)]
    public string RemotePrivateKeyString { get; set; } = string.Empty;

    [SimpleOption("ContainerPort", Description = "Port number associated with the container")]
    public ushort ContainerPort { get; init; } = NetConstants.MinPort;

    public void Prepare()
    {
        if (SeedKey.TryParse(this.RemotePrivateKeyString, out var seedKey))
        {
            this.RemoteSeedKey = seedKey;
        }

        this.RemotePrivateKeyString = string.Empty;
    }

    public SeedKey? RemoteSeedKey { get; private set; }
}
