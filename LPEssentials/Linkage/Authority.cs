﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

/// <summary>
/// Immutable authority (name + public key).
/// </summary>
[TinyhandObject]
public partial class Authority : IValidatable // , IEquatable<Authority>, IComparable<Authority>
{
    public const int NameLength = 16;
    public const string ECCurveName = "secp256r1";
    public const int PublicKeySize = 64;
    public const int PrivateKeySize = 32;

    public Authority()
    {
        this.Name = default!;
        this.X = default!;
        this.Y = default!;
    }

    public Authority(string name)
    {
        this.Name = name;
        this.X = default!;
        this.Y = default!;
    }

    [Key(0)]
    public string Name { get; private set; }

    [Key(1)]
    public byte[] X { get; private set; }

    [Key(2)]
    public byte[] Y { get; private set; }
}
