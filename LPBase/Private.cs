// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

namespace LP;

public class Private
{
    public Private()
    {
        Radio.Open<Message.Configure>(this.Configure);
    }

    public void Configure(Message.Configure message)
    {
        var ecdh = NodeKey.FromPrivateKey(this.NodePrivateKey);
        if (ecdh != null)
        {
            this.NodePrivateEcdh = ecdh;
        }
        else
        {
            var nodePrivateKey = NodePrivateKey.Create();
            this.NodePrivateEcdh = NodeKey.FromPrivateKey(nodePrivateKey)!;
        }
    }

    public NodePrivateKey NodePrivateKey { get; set; } = default!;

    public ECDiffieHellman NodePrivateEcdh { get; set; } = default!;
}
