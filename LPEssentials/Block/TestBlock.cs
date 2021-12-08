﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Blocks;

[TinyhandObject]
public partial class TestBlock : IBlock
{
    public static TestBlock Create()
    {
        var testBlock = new TestBlock();
        testBlock.N = 10;
        testBlock.Message = "Test message";
        testBlock.Data = new byte[4_000_000];
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

    [Key(2)]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public uint Id => 0xd75af226;

    public override string ToString()
        => $"TestBlock: {this.N}, {this.Message}, Size:{this.Data.Length}, Hash:{Arc.Crypto.FarmHash.Hash64(this.Data).To4Hex()}";
}
