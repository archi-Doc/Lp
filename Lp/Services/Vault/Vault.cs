// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Collections;

namespace Lp.Services;

[TinyhandObject(UseServiceProvider = true)]
public sealed partial class Vault
{
    [TinyhandObject]
    private readonly partial struct Item
    {
        public Item(byte[]? data, Vault? vault)
        {
            this.Data = data;
            this.Vault = vault;
        }

        public Item Clone(byte[] plaintext)
            => new(plaintext, this.Vault);

        public Item Clone(Vault vaultData)
            => new(this.Data, vaultData);

        [Key(0)]
        public readonly byte[]? Data;

        [Key(1)]
        public readonly Vault? Vault;
    }

    #region FieldAndProperty

    private readonly VaultControl vaultControl;
    private readonly Lock lockObject = new();
    private readonly OrderedMap<string, Item> nameToItem = new();
    private string password = string.Empty;

    #endregion

    public Vault(VaultControl vaultControl)
    {
        this.vaultControl = vaultControl;
    }

    public VaultResult TryAdd(string name, byte[] plaintext)
    {
        using (this.lockObject.EnterScope())
        {
            this.nameToItem.TryGetValue(name, out var item);
            if (item.Data is not null)
            {// Already exists.
                return VaultResult.AlreadyExists;
            }

            this.nameToItem.Add(name, item.Clone(plaintext));
            return VaultResult.Success;
        }
    }

    public void Add(string name, byte[] plaintext)
    {
        using (this.lockObject.EnterScope())
        {
            this.nameToItem.TryGetValue(name, out var item);
            this.nameToItem.Add(name, item.Clone(plaintext));
        }
    }

    public VaultResult SerializeAndTryAdd<T>(string name, T obj)
    {
        var bytes = TinyhandSerializer.Serialize<T>(obj);
        return this.TryAdd(name, bytes);
    }

    public void SerializeAndAdd<T>(string name, T obj)
    {
        var bytes = TinyhandSerializer.Serialize<T>(obj);
        this.Add(name, bytes);
    }

    public VaultResult ConvertAndTryAdd<T>(string name, T obj)
        where T : IStringConvertible<T>
    {
        var array = Arc.Crypto.CryptoHelper.ConvertToUtf8(obj);
        if (array.Length == 0)
        {
            return VaultResult.ConvertFailure;
        }

        return this.TryAdd(name, array);
    }

    public bool Exists(string name)
    {
        using (this.lockObject.EnterScope())
        {
            return this.nameToItem.ContainsKey(name);
        }
    }

    public bool Remove(string name)
    {
        using (this.lockObject.EnterScope())
        {
            return this.nameToItem.Remove(name);
        }
    }

    public bool TryGet(string name, [MaybeNullWhen(false)] out byte[] plaintext)
    {
        using (this.lockObject.EnterScope())
        {
            if (!this.nameToItem.TryGetValue(name, out var item))
            {// Not found
                plaintext = null;
                return false;
            }

            plaintext = item.Data;
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
        using (this.lockObject.EnterScope())
        {
            return this.nameToItem.Select(x => x.Key).ToArray();
        }
    }

    public string[] GetNames(string prefix)
    {
        using (this.lockObject.EnterScope())
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
        using (this.lockObject.EnterScope())
        {
            //this.Created = true;
            this.password = password;
            this.nameToItem.Clear();
        }
    }

    public bool CheckPassword(string password)
    {
        using (this.lockObject.EnterScope())
        {
            return this.password == password;
        }
    }

    public bool ChangePassword(string currentPassword, string newPassword)
    {
        using (this.lockObject.EnterScope())
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
