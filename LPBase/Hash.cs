// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Crypto;

namespace LP;

/// <summary>
/// LP Hash (SHA3-256) class.
/// </summary>
public class Hash : SHA3_256
{
    public Identifier GetIdentifier(ReadOnlySpan<byte> input)
    {
        var b = this.GetHash(input);
        return new Identifier(b);
    }

    public Identifier IdentifierFinal()
    {
        var b = this.HashFinal();
        return new Identifier(b);
    }
}
