// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;

namespace Netsphere;

public class NetBase : UnitBase, IUnitPreparable
{
    public NetBase(UnitContext context)
        : base(context)
    {
    }

    public void Prepare(UnitMessage.Prepare message)
    {
        // Set port number
        if (this.NetsphereOptions.Port < NetControl.MinPort ||
            this.NetsphereOptions.Port > NetControl.MaxPort)
        {
            var showWarning = false;
            if (this.NetsphereOptions.Port != 0)
            {
                showWarning = true;
            }

            this.NetsphereOptions.Port = LP.Random.Pseudo.NextInt32(NetControl.MinPort, NetControl.MaxPort + 1);
            if (showWarning)
            {
                Logger.Default.Warning($"Port number must be between {NetControl.MinPort} and {NetControl.MaxPort}");
                Logger.Default.Information($"Port number is set to {this.NetsphereOptions.Port}");
            }
        }

        // Node key
        if (this.NodePrivateKey == null)
        {
            this.NodePrivateKey = NodePrivateKey.Create();
            this.NodePrivateEcdh = NodeKey.FromPrivateKey(this.NodePrivateKey)!;
            this.NodePublicKey = new NodePublicKey(this.NodePrivateKey);
            this.NodePublicEcdh = NodeKey.FromPublicKey(this.NodePublicKey.X, this.NodePublicKey.Y)!;
        }
    }

    public bool EnableServer { get; private set; }

    public string NodeName { get; private set; } = default!;

    public NetsphereOptions NetsphereOptions { get; private set; } = default!;

    public bool AllowUnsafeConnection { get; set; } = false;

    public NodePublicKey NodePublicKey { get; private set; } = default!;

    public ECDiffieHellman NodePublicEcdh { get; private set; } = default!;

    public class LogFlag
    {
        public bool FlowControl { get; set; }
    }

    public LogFlag Log { get; } = new();

    public void SetParameter(bool enableServer, string nodeName, NetsphereOptions netsphereOptions)
    {
        this.EnableServer = enableServer;
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

    public byte[] SerializeNodeKey()
    {
        return TinyhandSerializer.Serialize(this.NodePrivateKey);
    }

    public override string ToString() => $"NetBase: {this.NodeName}";

    internal NodePrivateKey NodePrivateKey { get; private set; } = default!;

    internal ECDiffieHellman NodePrivateEcdh { get; private set; } = default!;
}
