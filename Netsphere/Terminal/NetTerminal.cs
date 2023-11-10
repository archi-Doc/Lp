// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;
using Arc.Crypto;
using LP.T3CS;

namespace Netsphere;

#pragma warning disable SA1401 // Fields should be private

/// <summary>
/// Initializes a new instance of the <see cref="NetTerminal"/> class.<br/>
/// NOT thread-safe.
/// </summary>
[ValueLinkObject]
public partial class NetTerminal : IDisposable
{
    /// <summary>
    /// The default interval time in milliseconds.
    /// </summary>
    public const int DefaultInterval = 10;

    private const long EventIdMask = 0xFFFF;

    private class NetTerminalLogger : ILog
    {
        public NetTerminalLogger(ILog log, NetTerminal netTerminal)
        {
            this.log = log;
            this.netTerminal = netTerminal;
        }

        Type ILog.OutputType => throw new NotImplementedException();

        void ILog.Log(long eventId, string message, Exception? exception)
        {
            this.log.Log((long)this.netTerminal.Salt & EventIdMask, message, exception);
        }

        private readonly ILog log;
        private readonly NetTerminal netTerminal;
    }

    [Link(Type = ChainType.QueueList, Name = "Queue", Primary = true)]
    internal NetTerminal(Terminal terminal, NetEndPoint endPoint)
    {// NodeAddress: Unmanaged
        this.Terminal = terminal;
        this.GenePool = new(RandomVault.Crypto.NextUInt64());
        this.Endpoint = endPoint;

        this.FlowControl = new(this);

        this.Initialize();
    }

    internal NetTerminal(Terminal terminal, NetEndPoint endPoint, NetNode node, ulong gene)
    {// NodeInformation: Encrypted
        this.Terminal = terminal;
        this.GenePool = new(gene);
        this.Endpoint = endPoint;
        this.Node = node;

        this.FlowControl = new(this);

        this.Initialize();
    }

    public override int GetHashCode()
        => HashCode.Combine(this.Endpoint);

    public void SetMaximumResponseTime(int milliseconds = 1000)
    {
        this.maximumResponseMics = Mics.FromMilliseconds(milliseconds);
    }

    public long MaximumResponseMics => this.maximumResponseMics;

    public void SetMinimumBandwidth(double megabytesPerSecond = 0.1)
    {
        this.minimumBandwidth = megabytesPerSecond;
    }

    public double MinimumBandwidth => this.minimumBandwidth;

    public virtual async Task<NetResult> EncryptConnectionAsync() => NetResult.NoEncryptedConnection;

    public virtual void SendClose()
    {
    }

    public bool IsEncrypted => this.embryo != null;

    public bool IsSendComplete => false;

    public bool IsReceiveComplete => false;

    public bool IsClosed { get; internal set; }

    public NetEndPoint Endpoint { get; }

    public NetNode? Node { get; protected set; }

    public ulong Salt { get; private set; }

    public async ValueTask<Token> CreateToken(Token.Type tokenType)
    {
        if (!this.IsEncrypted)
        {
            var result = await this.EncryptConnectionAsync().ConfigureAwait(false);
            if (result != NetResult.Success)
            {
                return Token.Invalid;
            }
        }

        return new Token(tokenType, this.Salt, Mics.GetCorrected() + Token.DefaultMics, Identifier.Zero);
    }

    public bool ValidateAndVerifyToken(Token token, SignaturePublicKey publicKey)
    {
        if (token.Salt != this.Salt)
        {
            return false;
        }

        return token.ValidateAndVerifyWithoutSalt(publicKey);
    }

    public bool ValidateAndVerifyToken(Token token)
    {
        if (token.Salt != this.Salt)
        {
            return false;
        }

        return token.ValidateAndVerifyWithoutPublicKey();
    }

    internal Terminal Terminal { get; }

    internal AsyncPulseEvent ReceiveEvent { get; } = new();

    internal FlowControl FlowControl { get; }

    internal void SetSalt(ulong saltA, ulong saltA2)
    {
        Span<byte> span = stackalloc byte[sizeof(ulong) * 2];
        var b = span;
        BitConverter.TryWriteBytes(b, saltA);
        b = b.Slice(sizeof(ulong));
        BitConverter.TryWriteBytes(b, saltA2);

        (var hash0, _, _, _) = Sha3Helper.Get256_UInt64(span);

        this.Logger?.Log($"-> {hash0 & EventIdMask:X4}");
        this.Salt = hash0;
    }

    internal void InternalClose()
    {
        this.IsClosed = true;
    }

    internal void MergeNodeInformation(NodeInformation nodeInformation)
    {
        this.NodeInformation = Netsphere.NodeInformation.Merge(this.NodeAddress, nodeInformation);
    }

    internal void CreateHeader(out PacketHeader header, ulong gene)
    {
        header = default;
        header.Gene = gene;
        header.Engagement = this.Endpoint.Engagement;
    }

    internal unsafe void SendAck(ulong gene)
    {
        this.CreateHeader(out var header, gene);
        header.Id = PacketId.Ack;

        var arrayOwner = PacketPool.Rent();
        fixed (byte* bp = arrayOwner.ByteArray)
        {
            *(PacketHeader*)bp = header;
        }

        this.Terminal.AddRawSend(this.Endpoint.EndPoint, arrayOwner.ToMemoryOwner(0, PacketService.HeaderSize));
    }

    internal void ProcessSend(long currentMics)
    {
        lock (this.SyncObject)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.FlowControl.Update(currentMics);
            this.FlowControl.RentSendCapacity(out var sendCapacity);
            // this.Terminal.TerminalLogger?.Information(sendCapacity.ToString());
            foreach (var x in this.activeInterfaces)
            {
                if (sendCapacity == 0)
                {
                    break;
                }

                x.ProcessSend(currentMics, ref sendCapacity);
            }

            this.FlowControl.ReturnSendCapacity(sendCapacity);

            // Send Ack
            if ((currentMics - this.lastSendingAckMics) > Mics.FromMilliseconds(NetConstants.SendingAckIntervalInMilliseconds))
            {
                this.lastSendingAckMics = currentMics;

                foreach (var x in this.activeInterfaces)
                {
                    x.ProcessSendingAck();
                }

                foreach (var x in this.disposedInterfaces)
                {
                    x.ProcessSendingAck();
                }
            }
        }
    }

    internal void Add(NetInterface netInterface)
    {
        lock (this.SyncObject)
        {
            this.activeInterfaces.Add(netInterface);

            if (netInterface.SendGenes != null && netInterface.SendGenes.Length == 1)
            {// Send immediately.
                var sendCapacity = 1;
                netInterface.ProcessSend(Mics.GetSystem(), ref sendCapacity);
            }
        }
    }

    internal bool RemoveInternal(NetInterface netInterface)
    {// lock (this.SyncObject)
        if (netInterface.DisposedMics == 0)
        {// Active
            return this.activeInterfaces.Remove(netInterface);
        }
        else
        {// Disposed
            return this.disposedInterfaces.Remove(netInterface);
        }
    }

    internal NetResult ReportResult(NetResult result)
    {
        return result;
    }

    internal object SyncObject { get; } = new();

    internal SemaphoreSlim ConnectionSemaphore { get; } = new(1, 1);

    internal GenePool GenePool { get; }

    internal NetResult CreateEmbryo(ulong salt, ulong salt2)
    {
        if (this.Node == null)
        {
            return NetResult.NoNodeInformation;
        }

        lock (this.SyncObject)
        {
            if (this.embryo != null)
            {
                return NetResult.Success;
            }

            // KeyMaterial
            var pair = new NodeKeyPair(this.Terminal.NodePrivateKey, this.Node.PublicKey);
            var material = pair.DeriveKeyMaterial();
            // var material = this.Terminal.NodePrivateKey.DeriveKeyMaterial(this.NodeInformation.PublicKey);
            if (material == null)
            {
                return NetResult.NoNodeInformation;
            }

            // this.Log($"Material {material[0]} ({salt.To4Hex()}/{salt2.To4Hex()}), {this.NodeInformation.PublicKeyX[0]}, {this.Terminal.NodePrivateKey.X[0]}");

            // ulong Salt, Salt2, byte[] material, ulong Salt, Salt2
            Span<byte> buffer = stackalloc byte[sizeof(ulong) + sizeof(ulong) + KeyHelper.PrivateKeyLength + sizeof(ulong) + sizeof(ulong)];
            var span = buffer;
            BitConverter.TryWriteBytes(span, salt);
            span = span.Slice(sizeof(ulong));
            BitConverter.TryWriteBytes(span, salt2);
            span = span.Slice(sizeof(ulong));
            material.AsSpan().CopyTo(span);
            span = span.Slice(KeyHelper.PrivateKeyLength);
            BitConverter.TryWriteBytes(span, salt);
            span = span.Slice(sizeof(ulong));
            BitConverter.TryWriteBytes(span, salt2);

            this.embryo = Sha3Helper.Get384_ByteArray(buffer);

            this.GenePool.SetEmbryo(this.embryo);
            this.Logger?.Log("Embryo created.");
            // this.Log($"First gene {this.GenePool.GetSequential().To4Hex()} ({salt.To4Hex()}/{salt2.To4Hex()})");

            // Aes
            this.aes = Aes.Create();
            this.aes.KeySize = 256;
            this.aes.Key = this.embryo.AsSpan(0, 32).ToArray();
        }

        return NetResult.Success;
    }

    internal bool TryClean(long currentMics)
    {
        var mics = currentMics - Mics.FromSeconds(2);
        List<NetInterface>? list = null;

        lock (this.SyncObject)
        {
            // Disposed interfaces
            foreach (var x in this.disposedInterfaces)
            {
                if (x.DisposedMics < mics)
                {
                    list ??= new();
                    list.Add(x);
                }
            }

            // Remove interfaces
            if (list != null)
            {
                foreach (var x in list)
                {
                    x.DisposeActual();
                }
            }

            if (currentMics > (this.LastResponseMics + this.MaximumResponseMics) &&
                this.activeInterfaces.Count == 0 &&
                this.disposedInterfaces.Count == 0)
            {// No net interface
                return true;
            }
        }

        return false;
    }

    internal void ActiveToDisposed(NetInterface netInterface)
    {// lock (this.SyncObject)
        this.activeInterfaces.Remove(netInterface);
        this.disposedInterfaces.Add(netInterface);
    }

    internal bool TryEncryptPacket(ByteArrayPool.MemoryOwner plain, ulong gene, out ByteArrayPool.MemoryOwner encrypted)
    {
        if (this.embryo == null || plain.Memory.Length < PacketService.HeaderSize)
        {
            encrypted = default;
            return false;
        }

        var span = plain.Memory.Span;
        var source = span.Slice(PacketService.HeaderSize);
        Span<byte> iv = stackalloc byte[16];
        BitConverter.TryWriteBytes(iv, gene);
        this.embryo.AsSpan(32, 8).CopyTo(iv.Slice(8));

        var packet = PacketPool.Rent();
        span.Slice(0, PacketService.HeaderSize).CopyTo(packet.ByteArray);

        this.aes!.TryEncryptCbc(source, iv, packet.ByteArray.AsSpan(PacketService.HeaderSize), out var written, PaddingMode.PKCS7);
        encrypted = packet.ToMemoryOwner(0, PacketService.HeaderSize + written);
        PacketService.InsertDataSize(encrypted.Memory, (ushort)written);

        return true;
    }

    internal bool TryDecryptPacket(ByteArrayPool.MemoryOwner encrypted, ulong gene, out ByteArrayPool.MemoryOwner destination)
    {
        if (this.embryo == null)
        {
            destination = default;
            return false;
        }

        Span<byte> iv = stackalloc byte[16];
        BitConverter.TryWriteBytes(iv, gene);
        this.embryo.AsSpan(32, 8).CopyTo(iv.Slice(8));

        var packet = PacketPool.Rent();
        this.aes!.TryDecryptCbc(encrypted.Memory.Span, iv, packet.ByteArray, out var written, PaddingMode.PKCS7);
        destination = packet.ToMemoryOwner(0, written);

        return true;
    }

    internal void ResetLastResponseMics() => this.lastResponseMics = Mics.GetSystem();

    internal void SetLastResponseMics(long mics) => this.lastResponseMics = mics;

    internal long LastResponseMics => this.lastResponseMics;

    internal GenePool? TryFork() => this.embryo == null ? null : this.GenePool.Fork(this.embryo);

    internal void IncrementResendCount() => Interlocked.Increment(ref this.resendCount);

    internal uint ResendCount => Volatile.Read(ref this.resendCount);

    internal ILog? Logger { get; private set; }

    internal bool Disposed => this.disposed;

    private void Clear()
    {// lock (this.SyncObject)
        foreach (var x in this.activeInterfaces)
        {
            x.Clear();
        }

        this.activeInterfaces.Clear();

        foreach (var x in this.disposedInterfaces)
        {
            x.Clear();
        }

        this.disposedInterfaces.Clear();
    }

    private void Initialize()
    {
        if (this.Terminal.NetBase.NetsphereOptions.EnableLogger &&
            this.Terminal.UnitLogger.GetLogger(this.GetType()) is { } logger &&
            logger.TryGet() is { } log)
        {
            this.Logger = new NetTerminalLogger(log, this);
        }

        this.SetMaximumResponseTime();
        this.SetMinimumBandwidth();
        this.ResetLastResponseMics();
    }

    protected List<NetInterface> activeInterfaces = new();
    protected List<NetInterface> disposedInterfaces = new();
    protected byte[]? embryo; // 48 bytes
    private Aes? aes;

    private long maximumResponseMics;
    private double minimumBandwidth;
    private long lastSendingAckMics;
    private long lastResponseMics;

    private uint resendCount;

    // private PacketService packetService = new();

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="NetTerminal"/> class.
    /// </summary>
    ~NetTerminal()
    {
        this.Dispose(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// free managed/native resources.
    /// </summary>
    /// <param name="disposing">true: free managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                // free managed resources.
                this.Logger?.Log("Dispose");
                if (this.IsEncrypted && !this.IsClosed)
                {// Close connection.
                    this.SendClose();
                }

                this.IsClosed = true;
                this.Terminal.TryRemove(this);
                lock (this.SyncObject)
                {
                    this.Clear();
                }

                this.ConnectionSemaphore.Dispose();

                // this.Logger?.Log("terminal disposed.");
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }

    #endregion
}
