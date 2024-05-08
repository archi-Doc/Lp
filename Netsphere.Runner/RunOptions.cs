// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Netsphere.Runner;

public partial record RunOptions
{
    [SimpleOption("lifespan", Description = "Time in seconds until the runner automatically shuts down (-1: infinite).")]
    public long Lifespan { get; init; } = 6; // tempcode

    [SimpleOption("port", Description = "Port number associated with the runner")]
    public int Port { get; set; }

    [SimpleOption("nodeprivatekey", Description = "Node private key")]
    public string NodePrivateKey { get; set; } = string.Empty;

    [SimpleOption("image", Description = "Container image")]
    public string Image { get; init; } = string.Empty;

    [SimpleOption("dockerparam", Description = "Parameters to be passed to the docker run command.")]
    public string DockerParameters { get; init; } = string.Empty;

    [SimpleOption("containerparam", Description = "Parameters to be passed to the container.")]
    public string ContainerParameters { get; init; } = string.Empty;
}
