// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using LP.Services;

namespace LP;

public class KeyVault
{
    private class Item
    {
        public Item(string name, byte[] decrypted)
        {
            this.Name = name;
            this.Decrypted = decrypted;
        }

        public string Name { get; set; } = string.Empty;

        public byte[] Decrypted { get; set; }
    }

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
            }
            else
            {// Password required.
                if (password == null)
                {// Enter password
RetryPassword:
                    var results = await this.UserInterfaceService.RequestPassword(Hashed.KeyVault.EnterPassword);
                    if (results == null)
                    {
                        throw new PanicException();
                    }

                    if (PasswordEncrypt.TryDecrypt(items[i]!.Encrypted, results, out decrypted))
                    {// Success
                        password = results;
                    }
                    else
                    {// Failure
                        await this.UserInterfaceService.Notify(UserInterfaceNotifyLevel.Warning, Hashed.Dialog.Password.NotMatch);
                        goto RetryPassword;
                    }
                }
                else
                {// Password already entered.
                    if (PasswordEncrypt.TryDecrypt(items[i]!.Encrypted, password, out decrypted))
                    {// Success
                    }
                    else
                    {// Failure
                        await this.UserInterfaceService.Notify(UserInterfaceNotifyLevel.Fatal, Hashed.Dialog.Password.NotMatch);
                        throw new PanicException();
                    }
                }
            }

            // item[i], decrypted
            this.TryAdd(items[i]!.Name, decrypted.ToArray());
        }

        return true;
    }

    public bool TryAdd(string name, byte[] decrypted)
    {
        lock (this.syncObject)
        {
            if (this.items.Find(x => x.Name == name) != null)
            {// Already exists.
                return false;
            }

            this.items.Add(new(name, decrypted));
            return true;
        }
    }

    public bool TryGet(string name, [MaybeNullWhen(false)] out byte[] decrypted)
    {
        lock (this.syncObject)
        {
            if (this.items.Find(x => x.Name == name) != null)
            {// Already exists.
                return false;
            }

            this.items.Add(new(name, decrypted));
            return true;
        }
    }

    public async Task SaveAsync(string path)
    {
        try
        {
            var items = this.GetEncrypted();
            var bytes = TinyhandSerializer.SerializeToUtf8(items);
            await File.WriteAllBytesAsync(path, bytes);
        }
        catch
        {
        }
    }

    public void Create(string password)
    {
        lock (this.syncObject)
        {
            this.Created = true;
            this.password = password;
            this.items.Clear();
        }
    }

    public IUserInterfaceService UserInterfaceService { get; }

    public bool Created { get; private set; } = false;

    private KeyVaultItem[] GetEncrypted()
    {
        KeyVaultItem[] array;
        var hint = PasswordEncrypt.GetPasswordHint(this.password);
        lock (this.syncObject)
        {
            array = new KeyVaultItem[this.items.Count];
            for (var i = 0; i < this.items.Count; i++)
            {
                var encrypted = PasswordEncrypt.Encrypt(this.items[i].Decrypted, this.password);
                array[i] = new(this.items[i].Name, hint, encrypted);
            }
        }

        return array;
    }

    private object syncObject = new();
    private string password = string.Empty;
    private List<Item> items = new();
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
