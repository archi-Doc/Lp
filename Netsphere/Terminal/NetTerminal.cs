// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;

namespace LP.Net;

[StructLayout(LayoutKind.Explicit)]
internal struct Packet_Header
{
    [FieldOffset(0)]
    public byte Engagement;

    [FieldOffset(1)]
    public byte Id;

    [FieldOffset(2)]
    public ulong Gene;
}

[ValueLinkObject]
public partial class NetTerminal : IDisposable
{
    /*internal struct Packet
    {
        public Packet(byte[] data)
        {
            this.Data = data;
        }

        public long CreatedTicks { get; } = Ticks.GetCurrent();

        public byte[] Data { get; }
    }*/

    internal NetTerminal(ulong gene, NodeAddress nodeAddress)
    {
        this.Gene = gene;
        this.NodeAddress = nodeAddress;
    }

    [Link(Type = ChainType.Ordered)]
    public ulong Gene { get; private set; }

    // [Link(Type = ChainType.Ordered)]
    // public long CreatedTicks { get; private set; } = Ticks.GetCurrent();

    public NodeAddress NodeAddress { get; }

    public unsafe void SendPunch()
    {
        var header = default(Packet_Header);
        header.Gene = this.Gene;
        header.Id = 1;

        int size = Marshal.SizeOf(header);
        var buffer = new byte[size];
        fixed (byte* pb = buffer)
        {
            *(Packet_Header*)pb = header;
        }

        this.SendRaw(buffer);
    }

    private bool SendRaw(byte[] buffer)
    {
        var original = Interlocked.CompareExchange(ref this.SendBuffer, buffer, null);
        if (original != null)
        {
            return false;
        }

        this.SendBufferTicks = Ticks.GetCurrent();
        // this.SendBuffer = buffer;
        return true;
    }

#pragma warning disable SA1401 // Fields should be private
    internal long SendBufferTicks;
    internal byte[]? SendBuffer;
#pragma warning restore SA1401 // Fields should be private

    private object syncObject = new();

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
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
