// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using SimpleCommandLine;

namespace Netsphere.Runner;

public partial record RunOptions
{
    public const string NodePrivateKeyName = "nodeprivatekey";
    public const string RemotePublicKeyName = "remotepublickey";

    [SimpleOption("lifespan", Description = "Time in seconds until the runner automatically shuts down (set to -1 for infinite).")]
    public long Lifespan { get; init; } = 6; // tempcode

    [SimpleOption("port", Description = "Port number associated with the runner")]
    public int Port { get; set; } = 49999;

    [SimpleOption(NodePrivateKeyName, Description = "Node private key for connection")]
    public string NodePrivateKeyString { get; set; } = string.Empty;

    [SimpleOption(RemotePublicKeyName, Description = "Public key for remote operation")]
    public string RemotePublicKeyString { get; set; } = string.Empty;

    [SimpleOption("image", Description = "Container image")]
    public string Image { get; init; } = string.Empty;

    [SimpleOption("dockerparam", Description = "Parameters to be passed to the docker run command.")]
    public string DockerParameters { get; init; } = string.Empty;

    [SimpleOption("containerparam", Description = "Parameters to be passed to the container.")]
    public string ContainerParameters { get; init; } = string.Empty;

    internal NodePrivateKey? NodePrivateKey { get; set; }

    internal SignaturePublicKey RemotePublicKey { get; set; }

    public void Prepare()
    {
        if (!string.IsNullOrEmpty(this.NodePrivateKeyString) &&
            NodePrivateKey.TryParse(this.NodePrivateKeyString, out var privateKey))
        {
            this.NodePrivateKey = privateKey;
        }

        if (!string.IsNullOrEmpty(this.RemotePublicKeyString) &&
            SignaturePublicKey.TryParse(this.RemotePublicKeyString, out var publicKey))
        {
            this.RemotePublicKey = publicKey;
        }
    }
}
