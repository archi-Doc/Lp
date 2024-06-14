// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Netsphere.Version;

public partial record ServerOptions
{
    [SimpleOption("port", Description = "Port number associated with the address")]
    public int Port { get; set; } = 55555;

    // [SimpleOption(NetConstants.NodePrivateKeyName, Description = "Node private key for connection", GetEnvironmentVariable = true)]
    // public string NodePrivateKeyString { get; set; } = string.Empty;

    [SimpleOption(NetConstants.RemotePublicKeyName, Description = "Public key for remote operation", GetEnvironmentVariable = true)]
    public string RemotePublicKeyString { get; set; } = string.Empty;

    [SimpleOption("version_identifier", Description = "Version identifier", GetEnvironmentVariable = true)]
    public int VersionIdentifier { get; set; }

    public bool Check(ILogger logger)
    {
        var result = true;

        if (!SignaturePublicKey.TryParse(this.RemotePublicKeyString, out var remotePublicKey))
        {
            logger.TryGet(LogLevel.Fatal)?.Log($"Specify the remote public key (-{NetConstants.RemotePublicKeyName}) for authentication of remote operations.");
            result = false;
        }

        this.RemotePublicKey = remotePublicKey;

        return result;
    }

    internal SignaturePublicKey RemotePublicKey { get; private set; }
}
