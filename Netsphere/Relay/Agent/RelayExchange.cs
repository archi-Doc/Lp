// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Netsphere.Relay;

[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
internal partial class RelayExchange
{
    [Link(Primary = true, Name = "LinkedList", Type = ChainType.LinkedList)]
    public RelayExchange(IRelayControl relayControl, RelayId innerRelayId, RelayId outerRelayId, ServerConnection serverConnection, AssignRelayBlock block)
    {
        this.InnerRelayId = innerRelayId;
        this.OuterRelayId = outerRelayId;
        this.Endpoint = serverConnection.DestinationEndpoint;
        this.LastAccessMics = Mics.FastSystem;

        this.EmbryoKey = new byte[32];
        serverConnection.EmbryoKey.CopyTo(this.EmbryoKey);
        this.EmbryoSalt = serverConnection.EmbryoSalt;
        this.EmbryoSecret = serverConnection.EmbryoSecret;

        this.RelayRetensionMics = relayControl.DefaultRelayRetensionMics;
        this.RestrictedIntervalMics = relayControl.DefaultRestrictedIntervalMics;
        this.AllowUnknownNode = block.AllowUnknownNode;
    }

    [Link(Type = ChainType.Unordered, AddValue = false)]
    public RelayId InnerRelayId { get; private set; }

    [Link(UnsafeTargetChain = "InnerRelayIdChain")]
    public RelayId OuterRelayId { get; private set; }

    public NetEndpoint Endpoint { get; private set; }

    public NetEndpoint OuterEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the remaining relay points.
    /// </summary>
    public long RelayPoint { get; internal set; }

    public long LastAccessMics { get; private set; }

    /// <summary>
    /// Gets the duration for maintaining the relay circuit.<br/>
    /// If no packets are received during this time, the relay circuit will be released.
    /// </summary>
    public long RelayRetensionMics { get; private set; }

    /// <summary>
    /// Gets the reception interval for packets from restricted nodes (unknown nodes) To protect the relay.<br/>
    /// Packets received more frequently than this interval will be discarded.<br/>
    /// If set to 0, all packets from unknown nodes will be discarded.
    /// </summary>
    public long RestrictedIntervalMics { get; private set; }

    public bool AllowUnknownNode { get; private set; }

    internal byte[] EmbryoKey { get; private set; }

    internal ulong EmbryoSalt { get; private set; }

    internal ulong EmbryoSecret { get; private set; }

    internal byte[] RelayKeyAndNonce32 { get; private set; }

    public bool DecrementAndCheck()
    {// using (items.LockObject.EnterScope())
        if (this.RelayPoint-- <= 0)
        {// All RelayPoints have been exhausted.
            this.Clean();
            this.Goshujin = null;
            return false;
        }
        else
        {
            this.LastAccessMics = Mics.FastSystem;
            return true;
        }
    }

    public void Clean()
    {
    }

    public unsafe void Encrypt(Span<byte> plaintext, uint salt4)
    {
        fixed (byte* pointer = plaintext)
        {
            var ciphertext = new Span<byte>(pointer, plaintext.Length + Aegis128L.MinTagSize);
            Span<byte> nonce16 = stackalloc byte[Aegis128L.NonceSize];
            this.CreateNonce(nonce16, salt4);

            Aegis128L.Encrypt(ciphertext, plaintext, nonce16, this.RelayKey);
        }
    }

    public unsafe bool TryDecrypt(Span<byte> ciphertext, uint salt4)
    {
        Debug.Assert(ciphertext.Length >= Aegis128L.MinTagSize);
        Span<byte> nonce16 = stackalloc byte[Aegis128L.NonceSize];
        this.CreateNonce(nonce16, salt4);

        return Aegis128L.TryDecrypt(ciphertext.Slice(0, ciphertext.Length - Aegis128L.MinTagSize), ciphertext, nonce16, this.RelayKey);
    }

    private ReadOnlySpan<byte> RelayKey => this.RelayKeyAndNonce32.AsSpan(0, Aegis128L.KeySize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CreateNonce(Span<byte> nonce16, uint salt4)
    {
        Debug.Assert(nonce16.Length == Aegis128L.NonceSize);

        this.RelayKeyAndNonce32.AsSpan(Aegis128L.KeySize, Aegis128L.NonceSize).CopyTo(nonce16);
        MemoryMarshal.AsRef<uint>(nonce16) ^= salt4;
    }
}
