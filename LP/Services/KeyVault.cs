// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Services;

namespace LP;

public class KeyVault
{
    public KeyVault(IUserInterfaceService userInterfaceService)
    {
        this.UserInterfaceService = userInterfaceService;
    }

    public async Task<bool> LoadAsync(string path)
    {
        byte[] data;
        try
        {
            data = await File.ReadAllBytesAsync(path);
        }
        catch
        {
            Logger.Default.Error(HashedString.Get(Hashed.Error.Load, path));
            return false;
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
            Logger.Default.Error(HashedString.Get(Hashed.Error.Deserialize, path));
            return false;
        }

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
                    var results = await this.UserInterfaceService.RequestString(Hashed.Services.KeyVault.EnterPassword);
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

        return true;
    }

    public IUserInterfaceService UserInterfaceService { get; }

    public bool NewKeyVault { get; set; } = false;
}

[TinyhandObject]
public partial class KeyVaultItem
{
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
