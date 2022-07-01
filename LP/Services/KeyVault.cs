// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Services;
using LPEssentials.Radio;

namespace LP;

public class KeyVault
{
    public static async Task<KeyVault?> Load(IViewService viewService, string path)
    {
        byte[] data;
        try
        {
            data = await File.ReadAllBytesAsync(path);
        }
        catch
        {
            Logger.Default.Error(HashedString.Get(Hashed.General.Error.Load, path));
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
            Logger.Default.Error(HashedString.Get(Hashed.General.Error.Deserialize, path));
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
                    var results = await viewService.RequestString(Hashed.Services.KeyVault.EnterPassword);
                    if (results != null)
                    {
                        password = results;
                    }
                }
            }
        }

        if (items.Length > 0)
        {
        }

        return keyVault;
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
