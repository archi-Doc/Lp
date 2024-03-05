// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Netsphere;

public class NetBase : UnitBase, IUnitPreparable
{
    public NetBase(UnitContext context, UnitLogger logger)
        : base(context)
    {
        this.UnitLogger = logger;

        this.NetOptions = new();
        this.NetOptions.NodeName = System.Environment.OSVersion.ToString();
        this.NewServerConnectionContext = connection => new ServerConnectionContext(connection);
        this.NewClientConnectionContext = connection => new ClientConnectionContext(connection);
    }

    #region FieldAndProperty

    public ThreadCoreBase Core => ThreadCore.Root;

    public UnitLogger UnitLogger { get; }

    public CancellationToken CancellationToken => this.Core.CancellationToken;

    public NetOptions NetOptions { get; private set; }

    public bool AllowUnsafeConnection { get; set; } = false;

    public ConnectionAgreement DefaultAgreement { get; set; } = ConnectionAgreement.Default;

    public TimeSpan DefaultSendTimeout { get; set; } = NetConstants.DefaultSendTimeout;

    public NodePublicKey NodePublicKey { get; private set; }

    internal NodePrivateKey NodePrivateKey { get; private set; } = default!;

    internal Func<ServerConnection, ServerConnectionContext> NewServerConnectionContext { get; set; }

    internal Func<ClientConnection, ClientConnectionContext> NewClientConnectionContext { get; set; }

    #endregion

    public void Prepare(UnitMessage.Prepare message)
    {
        // Set port number
        if (this.NetOptions.Port < NetConstants.MinPort ||
            this.NetOptions.Port > NetConstants.MaxPort)
        {
            var showWarning = false;
            if (this.NetOptions.Port != 0)
            {
                showWarning = true;
            }

            this.NetOptions.Port = RandomVault.Pseudo.NextInt32(NetConstants.MinPort, NetConstants.MaxPort + 1);
            if (showWarning)
            {
                this.UnitLogger.TryGet<NetBase>(LogLevel.Fatal)?.Log($"Port number must be between {NetConstants.MinPort} and {NetConstants.MaxPort}");
                this.UnitLogger.TryGet<NetBase>(LogLevel.Fatal)?.Log($"Port number is set to {this.NetOptions.Port}");
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
        this.NetOptions = netsphereOptions;

        if (!string.IsNullOrEmpty(this.NetOptions.PrivateKey) &&
            NodePrivateKey.TryParse(this.NetOptions.PrivateKey, out var privateKey))
        {
            this.SetNodePrivateKey(privateKey);
        }

        this.NetOptions.PrivateKey = string.Empty; // Erase
    }

    public bool SetNodePrivateKey(NodePrivateKey privateKey)
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

    public byte[] SerializeNodePrivateKey()
    {
        return TinyhandSerializer.Serialize(this.NodePrivateKey);
    }
}
