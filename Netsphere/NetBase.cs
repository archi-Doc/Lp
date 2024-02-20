// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Netsphere;

public class NetBase : UnitBase, IUnitPreparable
{
    public NetBase(UnitContext context, UnitLogger logger)
        : base(context)
    {
        this.UnitLogger = logger;

        this.NetsphereOptions = new();
        this.NetsphereOptions.NodeName = System.Environment.OSVersion.ToString();
        this.NewServerConnectionContext = connection => new ServerConnectionContext(connection);
        this.NewClientConnectionContext = connection => new ClientConnectionContext(connection);
    }

    #region FieldAndProperty

    public ThreadCoreBase Core => ThreadCore.Root;

    public UnitLogger UnitLogger { get; }

    public CancellationToken CancellationToken => this.Core.CancellationToken;

    public NetOptions NetsphereOptions { get; private set; }

    public bool AllowUnsafeConnection { get; set; } = false;

    public ConnectionAgreement DefaultAgreement { get; set; } = ConnectionAgreement.Default;

    public TimeSpan DefaultSendTimeout { get; set; } = NetConstants.DefaultSendTimeout;

    public NodePublicKey NodePublicKey { get; private set; }

    internal NodePrivateKey NodePrivateKey { get; private set; } = default!;

    internal Func<ServerConnection, ServerConnectionContext> NewServerConnectionContext { get; set; }

    internal Func<ClientConnection, ClientConnectionContext> NewClientConnectionContext { get; set; }

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

    public void SetOptions(NetOptions netsphereOptions)
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
