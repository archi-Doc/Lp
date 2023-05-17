// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Collections;

namespace LP;

public partial class Vault
{
    public const string Filename = "Vault.tinyhand";

    public Vault(ILogger<Vault> logger, IUserInterfaceService userInterfaceService, Data data)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;

        // var crystal = crystalizer.GetOrCreateCrystal<Data>(CrystalConfiguration.SingleUtf8(true, new RelativeFileConfiguration(Filename)));
        this.data = data;
    }

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

    [TinyhandObject(ExplicitKeyOnly = true)]
    internal sealed partial class Data
    {
        public readonly object syncObject = new();

        [Key(0)]
        public KeyValueList<string, EncryptedItem> items = default!;

        public OrderedMap<string, DecryptedItem> nameToDecrypted = new();
    }

#pragma warning restore SA1401 // Fields should be private
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

    [TinyhandObject]
    internal partial struct DecryptedItem
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
    internal partial struct EncryptedItem
    {
        public EncryptedItem()
        {
            this.Hint = 0;
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
        lock (this.data.syncObject)
        {
            if (this.data.nameToDecrypted.ContainsKey(name))
            {// Already exists.
                return false;
            }

            this.data.nameToDecrypted.Add(name, new(decrypted));
            return true;
        }
    }

    public bool SerializeAndTryAdd<T>(string name, T obj)
    {
        var bytes = TinyhandSerializer.Serialize<T>(obj);
        return this.TryAdd(name, bytes);
    }

    public bool SerializeAndAdd<T>(string name, T obj)
    {
        var bytes = TinyhandSerializer.Serialize<T>(obj);
        return this.Add(name, bytes);
    }

    public bool Add(string name, byte[] decrypted)
    {
        lock (this.data.syncObject)
        {
            if (this.data.nameToDecrypted.TryGetValue(name, out var item))
            {
                this.data.nameToDecrypted.Remove(name);
            }

            this.data.nameToDecrypted.Add(name, new(decrypted));
            return true;
        }
    }

    public bool Exists(string name)
    {
        lock (this.data.syncObject)
        {
            return this.data.nameToDecrypted.ContainsKey(name);
        }
    }

    public bool Remove(string name)
    {
        lock (this.data.syncObject)
        {
            return this.data.nameToDecrypted.Remove(name);
        }
    }

    public bool TryGet(string name, [MaybeNullWhen(false)] out byte[] decrypted)
    {
        lock (this.data.syncObject)
        {
            if (!this.data.nameToDecrypted.TryGetValue(name, out var item))
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

        try
        {
            obj = TinyhandSerializer.Deserialize<T>(decrypted);
            return obj != null;
        }
        catch
        {
            obj = default;
            return false;
        }
    }

    public string[] GetNames()
    {
        lock (this.data.syncObject)
        {
            return this.data.nameToDecrypted.Select(x => x.Key).ToArray();
        }
    }

    public string[] GetNames(string prefix)
    {
        lock (this.data.syncObject)
        {
            (var lower, var upper) = this.data.nameToDecrypted.GetRange(prefix, prefix + "\uffff");
            if (lower == null || upper == null)
            {
                return Array.Empty<string>();
            }

            var list = new List<string>();
            while (lower != null)
            {
                // list.Add(node.Key.Substring(prefix.Length));
                list.Add(lower.Key);

                if (lower == upper)
                {
                    break;
                }
                else
                {
                    lower = lower.Next;
                }
            }

            return list.ToArray();
            // return this.nameToDecrypted.Where(x => x.Key.StartsWith(prefix)).Select(x => x.Key).ToArray();
        }
    }

    public async Task<bool> LoadAsync(string? lppass)
    {
        string? password = lppass;
        foreach (var x in this.data.items)
        {
            if (PasswordEncrypt.TryDecrypt(x.Value.Encrypted, string.Empty, out var decrypted))
            {// No password
            }
            else
            {// Password required.
RetryPassword:
                if (password == null)
                {// Enter password
                    password = await this.userInterfaceService.RequestPassword(Hashed.Vault.EnterPassword).ConfigureAwait(false);
                    if (password == null)
                    {
                        throw new PanicException();
                    }
                }

                if (PasswordEncrypt.TryDecrypt(x.Value.Encrypted, password, out decrypted))
                {// Success
                    this.password = password;
                }
                else
                {// Failure
                    password = null;
                    await this.userInterfaceService.Notify(LogLevel.Warning, Hashed.Dialog.Password.NotMatch).ConfigureAwait(false);
                    goto RetryPassword;
                }

                /*else
                {// Password already entered.
                    if (PasswordEncrypt.TryDecrypt(x.Value.Encrypted, password, out decrypted))
                    {// Success
                    }
                    else
                    {// Failure
                        await this.userInterfaceService.Notify(LogLevel.Fatal, Hashed.Vault.NoRestore, x.Key).ConfigureAwait(false);
                        throw new PanicException();
                    }
                }*/
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
        lock (this.data.syncObject)
        {
            this.Created = true;
            this.password = password;
            this.data.nameToDecrypted.Clear();
        }
    }

    public bool CheckPassword(string password)
    {
        lock (this.data.syncObject)
        {
            return this.password == password;
        }
    }

    public bool ChangePassword(string currentPassword, string newPassword)
    {
        lock (this.data.syncObject)
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
        lock (this.data.syncObject)
        {
            var list = new KeyValueList<string, EncryptedItem>();
            foreach (var x in this.data.nameToDecrypted)
            {
                var encrypted = PasswordEncrypt.Encrypt(x.Value.Decrypted, this.password);
                list.Add(new(x.Key, new(hint, encrypted)));
            }

            return list;
        }
    }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private Data data;
    private string password = string.Empty;
}
