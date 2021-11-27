// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace
#pragma warning disable SA1208 // System using directives should be placed before other using directives
global using System;
global using System.IO;
global using Arc.Collections;
global using Arc.Crypto;
global using CrossChannel;
global using Tinyhand;

using System.Security.Cryptography;

namespace LP;

public class NetInformation
{
    public NetInformation()
    {
    }

    public string NodeName { get; private set; } = default!;

    public NodePublicKey NodePublicKey { get; set; } = default!;

    public ECDiffieHellman NodePublicEcdh { get; set; } = default!;

    public void Initialize(string nodeName)
    {
        this.NodeName = nodeName;
        if (string.IsNullOrEmpty(this.NodeName))
        {
            this.NodeName = System.Environment.OSVersion.ToString();
        }
    }

    public override string ToString()
    {
        return $"NetInformation: {this.NodeName}";
    }
}
