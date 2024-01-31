﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Server;

namespace Netsphere;

public class NetBase : UnitBase, IUnitPreparable
{
    public NetBase(UnitContext context, UnitLogger logger)
        : base(context)
    {
        this.UnitLogger = logger;
        this.ServerOptions = new();

        this.NetsphereOptions = new();
        this.NetsphereOptions.NodeName = System.Environment.OSVersion.ToString();
        this.NewConnectionContext = connection => new ConnectionContext(connection);
    }

    #region FieldAndProperty

    public ThreadCoreBase Core => ThreadCore.Root;

    public UnitLogger UnitLogger { get; }

    public CancellationToken CancellationToken => this.Core.CancellationToken;

    public NetsphereOptions NetsphereOptions { get; private set; }

    public Func<ServerConnection, ConnectionContext> NewConnectionContext { get; set; }

    public bool AllowUnsafeConnection { get; set; } = false;

    public ServerOptions ServerOptions { get; set; }

    public TimeSpan DefaultSendTimeout { get; set; } = NetConstants.DefaultSendTimeout;

    public NodePublicKey NodePublicKey { get; private set; }

    internal NodePrivateKey NodePrivateKey { get; private set; } = default!;

    public class LogFlag
    {
        public bool FlowControl { get; set; }
    }

    public LogFlag Log { get; } = new();

    #endregion

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

            this.NetsphereOptions.Port = RandomVault.Pseudo.NextInt32(NetControl.MinPort, NetControl.MaxPort + 1);
            if (showWarning)
            {
                this.UnitLogger.TryGet<NetBase>(LogLevel.Fatal)?.Log($"Port number must be between {NetControl.MinPort} and {NetControl.MaxPort}");
                this.UnitLogger.TryGet<NetBase>(LogLevel.Fatal)?.Log($"Port number is set to {this.NetsphereOptions.Port}");
            }
        }

        // Node key
        if (this.NodePrivateKey == null ||
            !this.NodePrivateKey.Validate())
        {
            this.NodePrivateKey = NodePrivateKey.Create();
            this.NodePublicKey = this.NodePrivateKey.ToPublicKey();
        }
    }

    public void SetOptions(NetsphereOptions netsphereOptions)
    {
        this.NetsphereOptions = netsphereOptions;
    }

    public bool SetNodeKey(NodePrivateKey privateKey)
    {
        try
        {
            this.NodePrivateKey = privateKey;
            this.NodePublicKey = privateKey.ToPublicKey();
            return true;
        }
        catch
        {
            this.NodePrivateKey = default!;
            this.NodePublicKey = default!;
            return false;
        }
    }

    public byte[] SerializeNodeKey()
    {
        return TinyhandSerializer.Serialize(this.NodePrivateKey);
    }

    public override string ToString() => $"NetBase: {this.NetsphereOptions.NodeName}";
}
