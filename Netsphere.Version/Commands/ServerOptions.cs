// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Net;
using Arc.Crypto;
using Arc.Unit;
using SimpleCommandLine;

namespace Netsphere.Version;

public partial record ServerOptions
{
    [SimpleOption("port", Description = "Port number associated with the address")]
    public int Port { get; set; }

    public bool Check(ILogger logger)
    {
        var result = true;
        /*if (this.RemotePublicKey.Equals(SignaturePublicKey.Default))
        {
            logger.TryGet(LogLevel.Fatal)?.Log($"Specify the remote public key (-{NetConstants.RemotePublicKeyName}) for authentication of remote operations.");
            result = false;
        }

        if (string.IsNullOrEmpty(this.Image))
        {
            logger.TryGet(LogLevel.Fatal)?.Log($"Specify the container image (-image).");
            result = false;
        }*/

        return result;
    }

    private NetNode? containerNode;
}
