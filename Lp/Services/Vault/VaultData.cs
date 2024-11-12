// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Collections;
using Lp.T3cs;

namespace Lp.Services;

[TinyhandObject(UseServiceProvider = true)]
public sealed partial class VaultData
{
    [TinyhandObject]
    private readonly partial struct Item
    {
        public Item(byte[]? plaintext, VaultData? vaultData)
        {
            this.Plaintext = plaintext;
            this.VaultData = vaultData;
        }

        public Item Clone(byte[] plaintext)
            => new(plaintext, this.VaultData);

        public Item Clone(VaultData vaultData)
            => new(this.Plaintext, vaultData);

        [Key(0)]
        public readonly byte[]? Plaintext;

        [Key(1)]
        public readonly VaultData? VaultData;
    }

    #region FieldAndProperty

    [Key(1)]
    public VaultLifecycle Lifecycle { get; private set; }

    [Key(2)]
    public long DecryptedMics { get; private set; }

    private readonly Vault vault;
    private readonly object syncObject = new();
    private readonly OrderedMap<string, Item> nameToItem = new();
    private string password = string.Empty;

    #endregion

    public VaultData(Vault vault)
    {
        this.vault = vault;
    }

    public bool TryAdd(string name, byte[] plaintext)
    {
        lock (this.syncObject)
        {
            this.nameToItem.TryGetValue(name, out var item);
            if (item.Plaintext is not null)
            {// Already exists.
                return false;
            }

            this.nameToItem.Add(name, item.Clone(plaintext));
            return true;
        }
    }

    public void Add(string name, byte[] plaintext)
    {
        lock (this.syncObject)
        {
            this.nameToItem.TryGetValue(name, out var item);
            this.nameToItem.Add(name, item.Clone(plaintext));
        }
    }

    public bool SerializeAndTryAdd<T>(string name, T obj)
    {
        var bytes = TinyhandSerializer.Serialize<T>(obj);
        return this.TryAdd(name, bytes);
    }

    public void SerializeAndAdd<T>(string name, T obj)
    {
        var bytes = TinyhandSerializer.Serialize<T>(obj);
        this.Add(name, bytes);
    }

    public bool FormatAndTryAdd<T>(string name, T obj)
        where T : IStringConvertible<T>
    {
        var array = Arc.Crypto.CryptoHelper.ConvertToUtf8(obj);
        if (array.Length == 0)
        {
            return false;
        }

        return this.TryAdd(name, array);
    }

    public bool Exists(string name)
    {
        lock (this.syncObject)
        {
            return this.nameToItem.ContainsKey(name);
        }
    }

    public bool Remove(string name)
    {
        lock (this.syncObject)
        {
            return this.nameToItem.Remove(name);
        }
    }

    public bool TryGet(string name, [MaybeNullWhen(false)] out byte[] plaintext)
    {
        lock (this.syncObject)
        {
            if (!this.nameToItem.TryGetValue(name, out var item))
            {// Not found
                plaintext = null;
                return false;
            }

            plaintext = item.Plaintext;
            return plaintext is not null;
        }
    }

    public bool TryGetAndDeserialize<T>(string name, [MaybeNullWhen(false)] out T obj)
    {
        if (!this.TryGet(name, out var plaintext))
        {
            obj = default;
            return false;
        }

        try
        {
            obj = TinyhandSerializer.Deserialize<T>(plaintext);
            return obj != null;
        }
        catch
        {
            obj = default;
            return false;
        }
    }

    public bool TryGetAndParse<T>(string name, [MaybeNullWhen(false)] out T obj)
        where T : IStringConvertible<T>
    {
        if (!this.TryGet(name, out var plaintext))
        {
            obj = default;
            return false;
        }

        return T.TryParse(System.Text.Encoding.UTF8.GetString(plaintext), out obj);
    }

    public string[] GetNames()
    {
        lock (this.syncObject)
        {
            return this.nameToItem.Select(x => x.Key).ToArray();
        }
    }

    public string[] GetNames(string prefix)
    {
        lock (this.syncObject)
        {
            (var lower, var upper) = this.nameToItem.GetRange(prefix, prefix + "\uffff");
            if (lower == null || upper == null)
            {
                return Array.Empty<string>();
            }

            var list = new List<string>();
            while (lower != null)
            {
                list.Add(lower.Key); // list.Add(node.Key.Substring(prefix.Length));

                if (lower == upper)
                {
                    break;
                }
                else
                {
                    lower = lower.Next;
                }
            }

            return list.ToArray(); // this.nameToItem.Where(x => x.Key.StartsWith(prefix)).Select(x => x.Key).ToArray();
        }
    }

    public void Create(string password)
    {
        lock (this.syncObject)
        {
            this.Created = true;
            this.password = password;
            this.nameToItem.Clear();
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

    public async Task SaveAsync()
    {//
        try
        {
            var b = PasswordEncryption.Encrypt(TinyhandSerializer.Serialize(this), this.password);
            await File.WriteAllBytesAsync(this.path, b).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    internal async Task LoadAsync()
    {
        if (this.vault.lpBase.IsFirstRun)
        {// First run
        }
        else
        {
            var result = await this.LoadAsync(this.vault.lpBase.Options.VaultPass).ConfigureAwait(false);
            if (result)
            {
                return;
            }

            // Could not load Vault
            var reply = await this.vault.userInterfaceService.RequestYesOrNo(Hashed.Vault.AskNew);
            if (reply != true)
            {// No
                throw new PanicException();
            }
        }

        this.vault.userInterfaceService.WriteLine(HashedString.Get(Hashed.Vault.Create));
        // await this.UserInterfaceService.Notify(UserInterfaceNotifyLevel.Information, Hashed.KeyVault.Create);

        // New Vault
        var password = this.vault.lpBase.Options.VaultPass;
        if (string.IsNullOrEmpty(password))
        {
            password = await this.userInterfaceService.RequestPasswordAndConfirm(Hashed.Vault.EnterPassword, Hashed.Dialog.Password.Confirm);
        }

        if (password == null)
        {
            throw new PanicException();
        }

        this.Create(password);
    }

    private async Task<bool> LoadAsync(string? lppass)
    {
        byte[] data;
        try
        {
            // data = this.vaultData.Data;
            data = await File.ReadAllBytesAsync(this.path).ConfigureAwait(false);
        }
        catch
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.Load, this.path);
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
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.Deserialize, this.path);
            return false;
        }

        string? password = lppass;
        foreach (var x in items)
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
}
