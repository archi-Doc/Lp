// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Packet;

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

    public UnitLogger UnitLogger { get; }

    public NetOptions NetOptions { get; private set; }

    public bool AllowUnsafeConnection { get; set; } = false;

    public ConnectionAgreement DefaultAgreement { get; set; } = ConnectionAgreement.Default;

    public TimeSpan DefaultTransmissionTimeout { get; set; } = NetConstants.DefaultTransmissionTimeout;

    public NodePublicKey NodePublicKey { get; private set; }

    internal NodePrivateKey NodePrivateKey { get; private set; } = default!;

    internal Func<ServerConnection, ServerConnectionContext> NewServerConnectionContext { get; set; }

    internal Func<ClientConnection, ClientConnectionContext> NewClientConnectionContext { get; set; }

    internal Func<ulong, PacketType, ReadOnlyMemory<byte>, BytePool.RentMemory?>? RespondPacketFunc { get; set; }

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
                this.UnitLogger.TryGet<NetBase>(LogLevel.Error)?.Log($"Port number must be between {NetConstants.MinPort} and {NetConstants.MaxPort}");
                this.UnitLogger.TryGet<NetBase>(LogLevel.Error)?.Log($"Port number is set to {this.NetOptions.Port}");
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

    public void SetRespondPacketFunc(Func<ulong, PacketType, ReadOnlyMemory<byte>, BytePool.RentMemory?> func)
    {
        this.RespondPacketFunc = func;
    }

    public void SetOptions(NetOptions netsphereOptions)
    {
        this.NetOptions = netsphereOptions;

        if (!string.IsNullOrEmpty(this.NetOptions.NodePrivateKey) &&
            NodePrivateKey.TryParse(this.NetOptions.NodePrivateKey, out var privateKey))
        {
            this.SetNodePrivateKey(privateKey);
        }

        this.NetOptions.NodePrivateKey = string.Empty; // Erase
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
