﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

public class KeyVault
{
    public static async Task<KeyVault?> Load(string path)
    {
        byte[] data;
        try
        {
            data = await File.ReadAllBytesAsync(path);
        }
        catch
        {
            Logger.Subcommand.Error($"Could not load '{path}'");
            return null;
        }

        KeyVaultItem?[]? items = null;
        try
        {
            items = TinyhandSerializer.DeserializeFromUtf8<KeyVaultItem[]>(data);
        }
        catch
        {
        }

        if (items == null)
        {
            Logger.Subcommand.Error($"Could not deserialize '{path}'");
            return null;
        }

        var keyVault = new KeyVault();
        string? password = null;
        for (var i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                continue;
            }

            if (PasswordEncrypt.TryDecrypt(items[i]!.Encrypted, string.Empty, out var decrypted))
            {// No password
                // keyVault.AddInternal(x.Name, decrypted);
                items[i] = null;
            }
            else
            {// Password required.
                if (password == null)
                {// Enter password

                }
            }
        }

        if (items.Length > 0)
        {
            
        }
    }

    public KeyVault()
    {
    }
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
