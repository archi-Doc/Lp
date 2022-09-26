// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using LP.Services;
using ValueLink;

namespace LP;

public partial class KeyVault2
{
    public const string Filename = "KeyVault.tinyhand";

    [TinyhandObject]
    public partial class Vault
    {
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
        private partial class EncryptedItem
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
            public byte Hint { get; protected set; }

            [KeyAsName]
            public byte[] Encrypted { get; protected set; } = Array.Empty<byte>();
        }

        public Vault(Vault? root)
        {
            this.root = root ?? this;
        }

        public bool IsRoot => this == this.root;

        public bool IsRemoved => true;

        private readonly object syncObject = new();

        private readonly Vault root;

        [Key(0)]
        private readonly SortedDictionary<string, DecryptedItem> nameToDecrypted = new();

        [Key(1)]
        private readonly List<Vault> children = new();

        private bool isRemoved = false;

        public bool TryAdd(string name, byte[] decrypted)
        {
            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return false;
                }

                if (this.nameToDecrypted.ContainsKey(name))
                {// Already exists.
                    return false;
                }

                this.nameToDecrypted.Add(name, new(decrypted));
                return true;
            }
        }

        public bool Add(string name, byte[] decrypted)
        {
            lock (this.syncObject)
            {
                if (this.IsRemoved)
                {
                    return false;
                }

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
                if (this.IsRemoved)
                {
                    decrypted = null;
                    return false;
                }

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
                if (this.IsRemoved)
                {
                    return Array.Empty<string>();
                }

                return this.nameToDecrypted.Select(x => x.Key).ToArray();
            }
        }
    }

    public KeyVault2(ILogger<KeyVault> logger, IUserInterfaceService userInterfaceService)
    {
        this.logger = logger;
        this.UserInterfaceService = userInterfaceService;
    }

    public Vault Root { get; } = new(null);

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

        KeyVaultItem?[]? items = null;
        try
        {
            items = TinyhandSerializer.DeserializeFromUtf8<KeyVaultItem[]>(data);
        }
        catch
        {
        }

        if (items == null || items.Length == 0)
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.Deserialize, path);
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
                    var results = await this.UserInterfaceService.RequestPassword(Hashed.KeyVault.EnterPassword).ConfigureAwait(false);
                    if (results == null)
                    {
                        throw new PanicException();
                    }

                    if (PasswordEncrypt.TryDecrypt(items[i]!.Encrypted, results, out decrypted))
                    {// Success
                        password = results;
                        this.password = password;
                    }
                    else
                    {// Failure
                        await this.UserInterfaceService.Notify(LogLevel.Warning, Hashed.Dialog.Password.NotMatch).ConfigureAwait(false);
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
                        await this.UserInterfaceService.Notify(LogLevel.Fatal, Hashed.KeyVault.NoRestore, items[i]!.Name).ConfigureAwait(false);
                        throw new PanicException();
                    }
                }
            }

            // item[i], decrypted
            this.TryAdd(items[i]!.Name, decrypted.ToArray());
        }

        return true;
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
            array = new KeyVaultItem[this.items.ListChain.Count];
            var i = 0;
            foreach (var x in this.items.ListChain)
            {
                var encrypted = PasswordEncrypt.Encrypt(x.Decrypted, this.password);
                array[i++] = new(x.Name, hint, encrypted);
            }
        }

        return array;
    }

    private ILogger<KeyVault> logger;
    private object syncObject = new();
    private string password = string.Empty;
    private Item.GoshujinClass items = new();
}
