// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace LP;

public class NetBase
{
    public NetBase()
    {
        Radio.Open<Message.Configure>(this.Configure);
    }

    public void Configure(Message.Configure message)
    {
        // Node key
        if (this.NodePrivateKey == null)
        {
            this.NodePrivateKey = NodePrivateKey.Create();
            this.NodePrivateEcdh = NodeKey.FromPrivateKey(this.NodePrivateKey)!;
            this.NodePublicKey = new NodePublicKey(this.NodePrivateKey);
            this.NodePublicEcdh = NodeKey.FromPublicKey(this.NodePublicKey.X, this.NodePublicKey.Y)!;
        }
    }

    public string NodeName { get; private set; } = default!;

    public NetsphereOptions NetsphereOptions { get; private set; } = default!;

    public NodePublicKey NodePublicKey { get; private set; } = default!;

    public ECDiffieHellman NodePublicEcdh { get; private set; } = default!;

    public void Initialize(string nodeName, NetsphereOptions netsphereOptions)
    {
        this.NodeName = nodeName;
        if (string.IsNullOrEmpty(this.NodeName))
        {
            this.NodeName = System.Environment.OSVersion.ToString();
        }

        this.NetsphereOptions = netsphereOptions;
    }

    public void SetNodeKey(NodePrivateKey privateKey)
    {
        this.NodePublicKey = new NodePublicKey(privateKey);
        this.NodePublicEcdh = NodeKey.FromPublicKey(this.NodePublicKey.X, this.NodePublicKey.Y) ?? throw new InvalidDataException();
        this.NodePrivateKey = privateKey;
        this.NodePrivateEcdh = NodeKey.FromPrivateKey(this.NodePrivateKey) ?? throw new InvalidDataException();
    }

    public override string ToString() => $"NetBase: {this.NodeName}";

    internal NodePrivateKey NodePrivateKey { get; private set; } = default!;

    internal ECDiffieHellman NodePrivateEcdh { get; private set; } = default!;
}
