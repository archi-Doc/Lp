// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using LP.Services;
using ValueLink;

namespace LP;

public partial class KeyVault
{
    public const string Filename = "KeyVault.tinyhand";

    public KeyVault(ILogger<KeyVault> logger, IUserInterfaceService userInterfaceService)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
    }

    [TinyhandObject]
    private partial struct DecryptedItem
    {
        public DecryptedItem()
        {
        }

        public DecryptedItem(byte[] decrypted)
        {
            this.Decrypted = decrypted;
        }

        [KeyAsName]
        internal byte[] Decrypted = Array.Empty<byte>();
    }

    [TinyhandObject]
    private partial struct EncryptedItem
    {
        public EncryptedItem()
        {
        }

        public EncryptedItem(int hint, byte[] encrypted)
        {
            this.Hint = (byte)hint;
            this.Encrypted = encrypted;
        }

        [KeyAsName]
        internal byte Hint;

        [KeyAsName]
        internal byte[] Encrypted = Array.Empty<byte>();
    }

    public bool TryAdd(string name, byte[] decrypted)
    {
        lock (this.syncObject)
        {
            if (this.nameToDecrypted.ContainsKey(name))
            {// Already exists.
                return false;
            }

            this.nameToDecrypted.Add(name, new(decrypted));
            return true;
        }
    }

    public bool SerializeAndTryAdd<T>(string name, T obj)
    {
        var bytes = TinyhandSerializer.Serialize<T>(obj);
        return this.TryAdd(name, bytes);
    }

    public bool Add(string name, byte[] decrypted)
    {
        lock (this.syncObject)
        {
            if (this.nameToDecrypted.TryGetValue(name, out var item))
            {
                this.nameToDecrypted.Remove(name);
            }

            this.nameToDecrypted.Add(name, new(decrypted));
            return true;
        }
    }

    public bool TryGet(string name, [MaybeNullWhen(false)] out byte[] decrypted)
    {
        lock (this.syncObject)
        {
            if (!this.nameToDecrypted.TryGetValue(name, out var item))
            {// Not found
                decrypted = null;
                return false;
            }

            decrypted = item.Decrypted;
            return true;
        }
    }

    public bool TryGetAndDeserialize<T>(string name, [MaybeNullWhen(false)] out T obj)
    {
        if (!this.TryGet(name, out var decrypted))
        {
            obj = default;
            return false;
        }

        obj = TinyhandSerializer.Deserialize<T>(decrypted);
        return obj != null;
    }

    public string[] GetNames()
    {
        lock (this.syncObject)
        {
            return this.nameToDecrypted.Select(x => x.Key).ToArray();
        }
    }

    public string[] GetNames(string prefix)
    {
        lock (this.syncObject)
        {
            return this.nameToDecrypted.Where(x => x.Key.StartsWith(prefix)).Select(x => x.Key).ToArray();
        }
    }

    public async Task<bool> LoadAsync(string path)
    {
        byte[] data;
        try
        {
            data = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
        }
        catch
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.Load, path);
            return false;
        }

        KeyValueList<string, EncryptedItem>? items = null;
        try
        {
            items = TinyhandSerializer.DeserializeFromUtf8<KeyValueList<string, EncryptedItem>>(data);
        }
        catch
        {
        }

        if (items == null)
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.Deserialize, path);
            return false;
        }

        string? password = null;
        foreach (var x in items)
        {
            if (PasswordEncrypt.TryDecrypt(x.Value.Encrypted, string.Empty, out var decrypted))
            {// No password
            }
            else
            {// Password required.
                if (password == null)
                {// Enter password
RetryPassword:
                    var results = await this.userInterfaceService.RequestPassword(Hashed.KeyVault.EnterPassword).ConfigureAwait(false);
                    if (results == null)
                    {
                        throw new PanicException();
                    }

                    if (PasswordEncrypt.TryDecrypt(x.Value.Encrypted, results, out decrypted))
                    {// Success
                        password = results;
                        this.password = password;
                    }
                    else
                    {// Failure
                        await this.userInterfaceService.Notify(LogLevel.Warning, Hashed.Dialog.Password.NotMatch).ConfigureAwait(false);
                        goto RetryPassword;
                    }
                }
                else
                {// Password already entered.
                    if (PasswordEncrypt.TryDecrypt(x.Value.Encrypted, password, out decrypted))
                    {// Success
                    }
                    else
                    {// Failure
                        await this.userInterfaceService.Notify(LogLevel.Fatal, Hashed.KeyVault.NoRestore, x.Key).ConfigureAwait(false);
                        throw new PanicException();
                    }
                }
            }

            // item[i], decrypted
            this.TryAdd(x.Key, decrypted.ToArray());
        }

        return true;
    }

    public async Task SaveAsync(string path)
    {
        try
        {
            var items = this.GetEncrypted();
            var bytes = TinyhandSerializer.SerializeToUtf8(items);
            await File.WriteAllBytesAsync(path, bytes).ConfigureAwait(false);
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
            this.nameToDecrypted.Clear();
        }
    }

    public bool CheckPassword(string password)
    {
        lock (this.syncObject)
        {
            return this.password == password;
        }
    }

    public bool ChangePassword(string currentPassword, string newPassword)
    {
        lock (this.syncObject)
        {
            if (this.password == currentPassword)
            {
                this.password = newPassword;
                return true;
            }
        }

        return false;
    }

    public bool Created { get; private set; } = false;

    private KeyValueList<string, EncryptedItem> GetEncrypted()
    {
        var hint = PasswordEncrypt.GetPasswordHint(this.password);
        lock (this.syncObject)
        {
            var list = new KeyValueList<string, EncryptedItem>();
            foreach (var x in this.nameToDecrypted)
            {
                var encrypted = PasswordEncrypt.Encrypt(x.Value.Decrypted, this.password);
                list.Add(new(x.Key, new(hint, encrypted)));
            }

            return list;
        }
    }

    private readonly object syncObject = new();
    private readonly ILogger<KeyVault> logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly SortedDictionary<string, DecryptedItem> nameToDecrypted = new();
    private string password = string.Empty;
}
