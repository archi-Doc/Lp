// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using Arc.Crypto;
using Arc.Unit;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Netsphere.Runner;

public partial record RunnerOptions
{
    // [SimpleOption("lifespan", Description = "Time in seconds until the runner automatically shuts down (set to -1 for infinite).")]
    // public long Lifespan { get; init; } = 6;

    [SimpleOption("port", Description = "Port number associated with the runner")]
    public ushort Port { get; set; } = 49999;

    [SimpleOption(NetConstants.NodePrivateKeyName, Description = "Node private key for connection", GetEnvironmentVariable = true)]
    public string NodePrivateKeyString { get; set; } = string.Empty;

    [SimpleOption(NetConstants.RemotePublicKeyName, Description = "Public key for remote operation", GetEnvironmentVariable = true)]
    public string RemotePublicKeyString { get; set; } = string.Empty;

    [SimpleOption("image", Description = "Container image")]
    public string Image { get; init; } = string.Empty;

    [SimpleOption("dockerparam", Description = "Parameters to be passed to the docker run command.")]
    public string DockerParameters { get; init; } = string.Empty;

    [SimpleOption("containerport", Description = "Port number associated with the container")]
    public ushort ContainerPort { get; set; } = 0; // 0: Disabled

    [SimpleOption("containerparam", Description = "Parameters to be passed to the container.")]
    public string ContainerParameters { get; init; } = string.Empty;

    public bool Check(ILogger logger)
    {
        var result = true;
        if (this.RemotePublicKey.Equals(SignaturePublicKey.Default))
        {
            logger.TryGet(LogLevel.Fatal)?.Log($"Specify the remote public key (-{NetConstants.RemotePublicKeyName}) for authentication of remote operations.");
            result = false;
        }

        if (string.IsNullOrEmpty(this.Image))
        {
            logger.TryGet(LogLevel.Fatal)?.Log($"Specify the container image (-image).");
            result = false;
        }

        return result;
    }

    public bool TryGetContainerAddress(out NetAddress netAddress)
    {
        if (this.ContainerPort == 0)
        {
            netAddress = default;
            return false;
        }

        netAddress = new NetAddress(IPAddress.Loopback, this.ContainerPort);
        return true;
    }

    public async ValueTask<NetNode?> TryGetContainerNode(NetTerminal netTerminal)
    {
        if (this.containerNode is not null)
        {
            return this.containerNode;
        }

        if (this.ContainerPort == 0)
        {
            return default;
        }

        var address = new NetAddress(IPAddress.Loopback, this.ContainerPort);
        this.containerNode = await netTerminal.UnsafeGetNetNode(address).ConfigureAwait(false);
        return this.containerNode;
    }

    public void Prepare()
    {
        // 1st Argument, 2nd: Environment variable
        if (!string.IsNullOrEmpty(this.NodePrivateKeyString) &&
            NodePrivateKey.TryParse(this.NodePrivateKeyString, out var privateKey))
        {
            this.NodePrivateKey = privateKey;
        }

        if (this.NodePrivateKey is null)
        {
            if (CryptoHelper.TryParseFromEnvironmentVariable<NodePrivateKey>(NetConstants.NodePublicKeyName, out privateKey))
            {
                this.NodePrivateKey = privateKey;
            }
        }

        if (!string.IsNullOrEmpty(this.RemotePublicKeyString) &&
            SignaturePublicKey.TryParse(this.RemotePublicKeyString, out var publicKey))
        {
            this.RemotePublicKey = publicKey;
        }

        if (this.RemotePublicKey.Equals(SignaturePublicKey.Default))
        {
            if (CryptoHelper.TryParseFromEnvironmentVariable<SignaturePublicKey>(NetConstants.RemotePublicKeyName, out publicKey))
            {
                this.RemotePublicKey = publicKey;
            }
        }
    }

    private NetNode? containerNode;

    internal NodePrivateKey? NodePrivateKey { get; set; }

    internal SignaturePublicKey RemotePublicKey { get; set; }
}
