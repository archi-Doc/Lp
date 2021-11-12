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
        this.NodePrivateEcdh = NodeKey.FromPrivateKey(this.NodePrivateKey);
    }

    public NodePrivateKey NodePrivateKey { get; set; } = default!;

    public ECDiffieHellman NodePrivateEcdh { get; set; } = default!;
}
