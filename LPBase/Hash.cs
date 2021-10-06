// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Collections;
using Arc.Crypto;

namespace LP;

public class Hash : SHA3_256
{
    public static readonly new string HashName = "SHA3-256";
    public static readonly new uint HashBits = 256;
    public static readonly uint HashBytes = HashBits / 8;

    public static ObjectPool<Hash> ObjectPool { get; } = new(static () => new Hash());

    public Identifier GetIdentifier(ReadOnlySpan<byte> input)
    {
        return new Identifier(this.GetHashULong(input));
    }

    public Identifier IdentifierFinal()
    {
        return new Identifier(this.HashFinalULong());
    }
}
