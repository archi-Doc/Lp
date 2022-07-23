// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1300 // Element should begin with upper-case letter

[TinyhandObject]
public partial class TestPacket : IPacket
{
    public static TestPacket Create(int size)
    {
        size = size < PacketService.SafeMaxPayloadSize ? size : PacketService.SafeMaxPayloadSize;

        var testBlock = new TestPacket();
        testBlock.N = 10;
        testBlock.Message = "Test message";
        testBlock.Data = new byte[size];
        for (var n = 0; n < testBlock.Data.Length; n++)
        {
            testBlock.Data[n] = (byte)n;
        }

        return testBlock;
    }

    [Key(0)]
    public int N { get; set; }

    [Key(1)]
    public string Message { get; set; } = string.Empty;

    [Key(2, PropertyName = "Data", PropertyAccessibility = PropertyAccessibility.ProtectedSetter)]
    [MaxLength(1000)]
    private byte[] _data = Array.Empty<byte>();

    public uint BlockId => 0xd75af226;

    public PacketId PacketId => PacketId.Test;

    public override string ToString()
        => $"TestPacket: {this.N}, {this.Message}, Size:{this.Data.Length}, Hash:{Arc.Crypto.FarmHash.Hash64(this.Data).To4Hex()}";
}
