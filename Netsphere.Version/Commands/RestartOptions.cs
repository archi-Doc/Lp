// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.


namespace LP.Subcommands;

public record RestartOptions
{
    [SimpleOption("node", Description = "Target nodes", Required = true)]
    public string Node { get; init; } = string.Empty;

    [SimpleOption(NetConstants.RemotePrivateKeyName, Description = "Private key for remote operation", GetEnvironmentVariable = true)]
    public string RemotePrivateKeyString { get; set; } = string.Empty;

    [SimpleOption("containerport", Description = "Port number associated with the container")]
    public ushort ContainerPort { get; init; } = NetConstants.MinPort;
}
