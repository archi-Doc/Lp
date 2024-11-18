// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using Arc.Collections;

namespace Lp.Services;

[TinyhandObject(UseServiceProvider = true, LockObject = nameof(lockObject))]
public sealed partial class Vault : ITinyhandSerializationCallback
{
    [TinyhandObject]
    private partial class Item
    {// Plaintext, Plaintext+Object, Ciphertext, Ciphertext+Vault
        public enum Kind
        {
            ByteArray,
            Object,
            Vault,
        }

        public Item()
        {
        }

        public Item(byte[] data)
            => this.Set(data);

        public Item(ITinyhandSerialize? @object)
            => this.Set(@object);

        public Item(Vault vault)
            => this.Set(vault);

        #region FieldAndProperty

        [Key(0)]
        public Kind ItemKind { get; set; }

        [Key(1)]
        public byte[]? ByteArray { get; set; }

        [IgnoreMember]
        public ITinyhandSerialize? Object { get; set; }

        #endregion

        internal void Set(byte[] data)
        {
            this.ItemKind = Kind.ByteArray;
            this.ByteArray = data;
            this.Object = default;
        }

        internal void Set(ITinyhandSerialize? @object)
        {
            this.ItemKind = Kind.Object;
            this.ByteArray = default;
            this.Object = @object;
        }

        internal void Set(Vault vault)
        {
            this.ItemKind = Kind.Vault;
            this.ByteArray = default;
            this.Object = (ITinyhandSerialize)vault;
        }
    }

    #region FieldAndProperty

    private readonly VaultControl vaultControl;
    private readonly Lock lockObject = new();

    [IgnoreMember]
    public bool ModifiedFlag { get; private set; } // Since encryption in Vault (Argon2id) is a resource-intensive process, a modification flag is used to ensure encryption and serialization occur only when changes are made.

    [Key(0)]
    private OrderedMap<string, Item> nameToItem = new();

    [IgnoreMember]
    private string password = string.Empty;

    #endregion

    public Vault(VaultControl vaultControl)
    {
        this.vaultControl = vaultControl;
    }

    public bool TryAddByteArray(string name, byte[] byteArray, out VaultResult result)
    {
        using (this.lockObject.EnterScope())
        {
            if (this.nameToItem.FindNode(name) is { } node)
            {// Already exists.
                result = VaultResult.AlreadyExists;
                return false;
            }
            else
            {
                this.nameToItem.Add(name, new Item(byteArray));
            }

            this.SetModifiedFlag();
            result = VaultResult.Success;
            return true;
        }
    }

    public void AddByteArray(string name, byte[] byteArray)
    {
        using (this.lockObject.EnterScope())
        {
            if (this.nameToItem.FindNode(name) is { } node)
            {// Already exists.
                node.Value.Set(byteArray);
            }
            else
            {// New
                this.nameToItem.Add(name, new Item(byteArray));
            }

            this.SetModifiedFlag();
        }
    }

    public bool TryGetByteArray(string name, [MaybeNullWhen(false)] out byte[] byteArray, out VaultResult result)
    {
        using (this.lockObject.EnterScope())
        {
            if (!this.nameToItem.TryGetValue(name, out var item))
            {// Not found
                byteArray = default;
                result = VaultResult.NotFound;
                return false;
            }
            else if (item.ItemKind != Item.Kind.ByteArray ||
                item.ByteArray is null)
            {// Kind mismatch
                byteArray = default;
                result = VaultResult.KindMismatch;
                return false;
            }

            byteArray = item.ByteArray;
            result = VaultResult.Success;
            return true;
        }
    }

    public bool TryAddObject(string name, ITinyhandSerialize @object, out VaultResult result)
    {
        using (this.lockObject.EnterScope())
        {
            if (this.nameToItem.FindNode(name) is { } node)
            {// Already exists.
                result = VaultResult.AlreadyExists;
                return false;
            }
            else
            {
                this.nameToItem.Add(name, new Item(@object));
            }

            this.SetModifiedFlag();
            result = VaultResult.Success;
            return true;
        }
    }

    public void AddObject(string name, ITinyhandSerialize @object)
    {
        using (this.lockObject.EnterScope())
        {
            if (this.nameToItem.FindNode(name) is { } node)
            {// Already exists.
                node.Value.Set(@object);
            }
            else
            {// New
                this.nameToItem.Add(name, new Item(@object));
            }

            this.SetModifiedFlag();
        }
    }

    public bool TryGetObject<TObject>(string name, [MaybeNullWhen(false)] out TObject @object, out VaultResult result)
        where TObject : class, ITinyhandSerialize<TObject>
    {
        using (this.lockObject.EnterScope())
        {
            if (!this.nameToItem.TryGetValue(name, out var item))
            {// Not found
                @object = default;
                result = VaultResult.NotFound;
                return false;
            }
            else if (item.ItemKind != Item.Kind.Object)
            {// Kind mismatch
                @object = default;
                result = VaultResult.KindMismatch;
                return false;
            }

            // Object instance
            @object = item.Object as TObject;
            if (@object is not null)
            {
                result = VaultResult.Success;
                return true;
            }

            // Deserialize
            if (item.ByteArray is null)
            {
                try
                {
                    @object = TinyhandSerializer.DeserializeObject<TObject>(item.ByteArray);
                    item.Object = @object as ITinyhandSerialize;
                }
                catch
                {
                }
            }

            if (@object is not null)
            {
                result = VaultResult.Success;
                return true;
            }
            else
            {
                result = VaultResult.InvalidData;
                return false;
            }
        }
    }

    public bool TryAddVault(string name, [MaybeNullWhen(false)] out Vault vault, out VaultResult result)
    {
        using (this.lockObject.EnterScope())
        {
            if (this.nameToItem.FindNode(name) is { } node)
            {// Already exists.
                vault = default;
                result = VaultResult.AlreadyExists;
                return false;
            }
            else
            {
                vault = new(this.vaultControl);
                this.nameToItem.Add(name, new Item(vault));
            }

            this.SetModifiedFlag();
            result = VaultResult.Success;
            return true;
        }
    }

    public void AddVault(string name, out Vault vault)
    {
        using (this.lockObject.EnterScope())
        {
            vault = new(this.vaultControl);
            if (this.nameToItem.FindNode(name) is { } node)
            {// Already exists.
                node.Value.Set(vault);
            }
            else
            {// New
                this.nameToItem.Add(name, new Item(vault));
            }

            this.SetModifiedFlag();
        }
    }

    public bool TryGetVault<TObject>(string name, string password, [MaybeNullWhen(false)] out Vault vault)
    {
        using (this.lockObject.EnterScope())
        {
            if (!this.nameToItem.TryGetValue(name, out var item))
            {// Not found
                vault = default;
                return false;
            }

            // Object instance
            vault = item.Object as Vault;
            if (vault is not null)
            {
                return true;
            }

            // Deserialize
            if (item.ByteArray is not null)
            {
                try
                {
                    if (!PasswordEncryption.TryDecrypt(item.ByteArray, password, out var plaintext))
                    {
                        vault = default;
                        return false;
                    }

                    var b = plaintext.ToArray();
                    item.Object = TinyhandSerializer.DeserializeObject<Vault>(b);
                }
                catch
                {
                }
            }

            return item.Object is not null;
        }
    }

    /*public VaultResult SerializeAndTryAdd<T>(string name, T obj)
    {
        var bytes = TinyhandSerializer.Serialize<T>(obj);
        return this.TryAddByteArray(name, bytes);
    }

    public void SerializeAndAdd<T>(string name, T obj)
    {
        var bytes = TinyhandSerializer.Serialize<T>(obj);
        this.AddByteArray(name, bytes);
    }

    public VaultResult ConvertAndTryAdd<T>(string name, T obj)
        where T : IStringConvertible<T>
    {
        var array = Arc.Crypto.CryptoHelper.ConvertToUtf8(obj);
        if (array.Length == 0)
        {
            return VaultResult.ConvertFailure;
        }

        return this.TryAddByteArray(name, array);
    }*/

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

    /*public bool TryGetAndDeserialize<T>(string name, [MaybeNullWhen(false)] out T obj)
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
    }*/

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

    public bool PasswordEquals(string password)
    {
        using (this.lockObject.EnterScope())
        {
            return this.password == password;
        }
    }

    public bool SetPassword(string newPassword)
    {
        using (this.lockObject.EnterScope())
        {
            // if (this.password == currentPassword)
            this.password = newPassword;
            this.SetModifiedFlag();
            return true;
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

    void ITinyhandSerializationCallback.OnAfterReconstruct()
    {
    }

    void ITinyhandSerializationCallback.OnAfterDeserialize()
    {
    }

    void ITinyhandSerializationCallback.OnBeforeSerialize()
    {
        List<string>? toDelete = default;
        foreach ((var key, var x) in this.nameToItem)
        {
            try
            {
                if (x.ItemKind == Item.Kind.Object)
                {// Object
                    var newByteArray = TinyhandSerializer.Serialize(x.Object);
                    if (x.ByteArray is null ||
                        !newByteArray.SequenceEqual(x.ByteArray))
                    {// Not identical
                        x.ByteArray = newByteArray;
                        this.SetModifiedFlag();
                    }
                }
                else if (x.ItemKind == Item.Kind.Vault)
                {// Vault
                    if (x.Object is Vault vault)
                    {
                        if (vault.TrySerialize() is { } byteArray)
                        {
                            x.ByteArray = byteArray;
                            vault.ResetModifiedFlag();
                        }
                    }
                    else
                    {// Invlaid
                        toDelete ??= new();
                        toDelete.Add(key);
                    }
                }
            }
            catch
            {
                toDelete ??= new();
                toDelete.Add(key);
            }
        }

        if (toDelete is not null)
        {// Delete invalid items.
            foreach (var x in toDelete)
            {
                this.nameToItem.Remove(x);
            }

            this.SetModifiedFlag();
        }
    }

    private void SetModifiedFlag() => this.ModifiedFlag = true;

    private void ResetModifiedFlag() => this.ModifiedFlag = false;

    private byte[]? TrySerialize()
    {
        if (this.ModifiedFlag)
        {// Not modified
            return default;
        }

        try
        {
            var plaintext = TinyhandSerializer.SerializeObject(this);
            PasswordEncryption.Encrypt(plaintext, this.password, out var ciphertext);
            return ciphertext;
        }
        catch
        {
            return default;
        }
    }
}
