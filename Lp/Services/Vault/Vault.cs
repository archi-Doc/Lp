﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Collections;

namespace Lp.Services;

[TinyhandObject(UseServiceProvider = true)]
public sealed partial class Vault
{
    [TinyhandObject]
    private readonly partial struct Item
    {
        public Item(byte[]? plaintext, Vault? vaultData)
        {
            this.Plaintext = plaintext;
            this.VaultData = vaultData;
        }

        public Item Clone(byte[] plaintext)
            => new(plaintext, this.VaultData);

        public Item Clone(Vault vaultData)
            => new(this.Plaintext, vaultData);

        [Key(0)]
        public readonly byte[]? Plaintext;

        [Key(1)]
        public readonly Vault? VaultData;
    }

    #region FieldAndProperty

    private readonly VaultControl vault;
    private readonly object syncObject = new();
    private readonly OrderedMap<string, Item> nameToItem = new();
    private string password = string.Empty;

    #endregion

    public Vault(VaultControl vault)
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
            //this.Created = true;
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

    /*public async Task SaveAsync()
    {
        try
        {
            var b = PasswordEncryption.Encrypt(TinyhandSerializer.Serialize(this), this.password);
            await File.WriteAllBytesAsync(this.path, b).ConfigureAwait(false);
        }
        catch
        {
        }
    }*/
}
