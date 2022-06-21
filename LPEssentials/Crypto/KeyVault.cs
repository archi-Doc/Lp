// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public class KeyVault
{
}

[TinyhandObject]
public partial class KeyVaultItem
{
    public const int HintLength = 2;

    public KeyVaultItem()
    {
    }

    public KeyVaultItem(string name, int hint, byte[] encrypted)
    {
        this.Name = name;
        // Span<byte> span = stackalloc byte[4];
        // BitConverter.TryWriteBytes(span, hint);
        // this.Hint = span.Slice(0, HintLength).ToArray();
        this.Hint = (ushort)hint;
        this.Encrypted = encrypted;
    }

    [KeyAsName]
    public string Name { get; protected set; } = string.Empty;

    [KeyAsName]
    public ushort Hint { get; protected set; }

    [KeyAsName]
    public byte[] Encrypted { get; protected set; } = Array.Empty<byte>();
}
