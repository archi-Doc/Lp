// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.Subcommands;

public record RestartOptions
{
    [SimpleOption("runner_node", Description = "Runner nodes", Required = true)]
    public string RunnerNode { get; init; } = string.Empty;

    [SimpleOption(NetConstants.RemotePrivateKeyName, Description = "Private key for remote operation", GetEnvironmentVariable = true)]
    public string RemotePrivateKeyString { get; set; } = string.Empty;

    [SimpleOption("container_port", Description = "Port number associated with the container")]
    public ushort ContainerPort { get; init; } = NetConstants.MinPort;

    public void Prepare()
    {
        if (SignaturePrivateKey.TryParse(this.RemotePrivateKeyString, out var privateKey))
        {
            this.RemotePrivateKey = privateKey;
        }

        this.RemotePrivateKeyString = string.Empty;
    }

    public SignaturePrivateKey? RemotePrivateKey { get; private set; }
}
