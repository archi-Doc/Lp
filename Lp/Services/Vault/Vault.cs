// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security;
using Arc.Collections;

namespace Lp.Services;

[TinyhandObject(UseServiceProvider = true, LockObject = nameof(lockObject))]
public sealed partial class Vault
{
    [TinyhandObject]
    private partial class Item
    {
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

        public Item(ITinyhandSerializable? @object)
            => this.Set(@object);

        public Item(Vault vault)
            => this.Set(vault);

        #region FieldAndProperty

        [Key(0)]
        public Kind ItemKind { get; set; }

        [Key(1)]
        public byte[]? ByteArray { get; set; }

        [IgnoreMember]
        public ITinyhandSerializable? Object { get; set; }

        #endregion

        internal void Set(byte[] data)
        {
            this.ItemKind = Kind.ByteArray;
            this.ByteArray = data;
            this.Object = default;
        }

        internal void Set(ITinyhandSerializable? @object)
        {
            this.ItemKind = Kind.Object;
            this.ByteArray = default;
            this.Object = @object;
        }

        internal void Set(Vault vault)
        {
            this.ItemKind = Kind.Vault;
            this.ByteArray = default;
            this.Object = (ITinyhandSerializable)vault;
        }
    }

    #region FieldAndProperty

    private readonly VaultControl vaultControl;
    private readonly Lock lockObject = new();

    [IgnoreMember]
    public Vault? ParentVault { get; private set; }

    [IgnoreMember]
    public bool ModifiedFlag { get; private set; } // Since encryption in Vault (Argon2id) is a resource-intensive process, a modification flag is used to ensure encryption and serialization occur only when changes are made.

    [Key(0)]
    private OrderedMap<string, Item> nameToItem = new(StringComparer.InvariantCultureIgnoreCase);

    [IgnoreMember]
    private string password = string.Empty; // Consider SecureString

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

    public bool TryAddObject(string name, ITinyhandSerializable @object, out VaultResult result)
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

    public void AddObject(string name, ITinyhandSerializable @object)
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
        where TObject : class, ITinyhandSerializable<TObject>
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
            if (item.ByteArray is not null)
            {
                try
                {
                    @object = TinyhandSerializer.DeserializeObject<TObject>(item.ByteArray);
                    item.Object = @object as ITinyhandSerializable;
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
                result = VaultResult.DeserializationFailure;
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
                vault.Initialize(this, string.Empty);
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
            vault.Initialize(this, string.Empty);
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

    /// <summary>
    /// Tries to get a vault by name and password.
    /// </summary>
    /// <param name="name">The name of the vault.</param>
    /// <param name="password">The password for the vault.
    /// <br/>null; it returns true if decryption has been performed and false if not (it does not check the password).<br/>
    /// not null; it returns true only if decryption with the provided password succeeds or the password matches.</param>
    /// <param name="vault">The retrieved vault if successful.</param>
    /// <param name="result">The result of the operation.</param>
    /// <returns>True if the vault is found and the password matches; otherwise, false.</returns>
    public bool TryGetVault(string name, string? password, [MaybeNullWhen(false)] out Vault vault, out VaultResult result)
    {
        using (this.lockObject.EnterScope())
        {
            if (!this.nameToItem.TryGetValue(name, out var item))
            {// Not found
                vault = default;
                result = VaultResult.NotFound;
                return false;
            }

            // Object instance
            vault = item.Object as Vault;
            if (password is null)
            {
                if (vault is null)
                {
                    result = VaultResult.PasswordRequired;
                    return false;
                }
                else
                {
                    result = VaultResult.Success;
                    return true;
                }
            }

            if (vault is not null)
            {
                if (vault.PasswordEquals(password))
                {
                    result = VaultResult.Success;
                    return true;
                }
                else
                {
                    result = VaultResult.PasswordMismatch;
                    vault = default;
                    return false;
                }
            }

            if (item.ByteArray is null)
            {
                result = VaultResult.NotFound;
                vault = default;
                return false;
            }

            // Decrypt
            if (!PasswordEncryption.TryDecrypt(item.ByteArray, password, out var plaintext))
            {// Decryption failed
                result = VaultResult.DecryptionFailure;
                vault = default;
                return false;
            }

            // Deserialize
            if (!TinyhandSerializer.TryDeserializeObject<Vault>(plaintext, out vault))
            {// Deserialize failed
                result = VaultResult.DeserializationFailure;
                vault = default;
                return false;
            }

            vault.Initialize(this, password);
            item.Object = vault;
            result = VaultResult.Success;
            return true;
        }
    }

    public bool Contains(string name)
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
            return this.RemoveInternal(name);
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

    public string[] GetNames()
    {
        using (this.lockObject.EnterScope())
        {
            return this.nameToItem.Select(x => x.Key).ToArray();
        }
    }

    public bool PasswordEquals(string password)
    {
        using (this.lockObject.EnterScope())
        {
            return this.password == password;
        }
    }

    public void SetPassword(string newPassword)
    {
        using (this.lockObject.EnterScope())
        {
            this.password = newPassword;
            this.SetModifiedFlag();
        }
    }

    internal byte[] SerializeVault()
    {
        var plaintext = TinyhandSerializer.SerializeObject(this);
        PasswordEncryption.Encrypt(plaintext, this.password, out var ciphertext);
        return ciphertext;
    }

    [TinyhandOnSerializing]
    private void OnBeforeSerialize()
    {
        List<string>? toDelete = default;
        foreach ((var key, var x) in this.nameToItem)
        {
            try
            {
                if (x.ItemKind == Item.Kind.Object)
                {// Object
                    if (x.Object is ITinyhandSerializable value)
                    {
                        var newByteArray = x.Object.Serialize();
                        if (x.ByteArray is null ||
                            !newByteArray.SequenceEqual(x.ByteArray))
                        {// Not identical
                            x.ByteArray = newByteArray;
                            this.SetModifiedFlag();
                        }
                    }
                    else if (x.ByteArray is null)
                    {// Invlaid
                        toDelete ??= new();
                        toDelete.Add(key);
                    }
                }
                else if (x.ItemKind == Item.Kind.Vault)
                {// Vault
                    if (x.Object is Vault vault)
                    {
                        if (x.ByteArray is null ||
                            vault.ModifiedFlag)
                        {// Modified
                            x.ByteArray = vault.SerializeVault();
                            vault.ResetModifiedFlag();
                            this.SetModifiedFlag();
                        }
                    }
                    else if (x.ByteArray is null)
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
                this.Remove(x);
            }

            this.SetModifiedFlag();
        }
    }

    private void Initialize(Vault? parentVault, string password)
    {
        this.ParentVault = parentVault;
        this.password = password;
    }

    private bool RemoveInternal(string name)
    {
        if (this.nameToItem.FindNode(name) is { } node)
        {
            if (node.Value.ItemKind == Item.Kind.Vault &&
                node.Value.Object is Vault vault)
            {
                vault.ParentVault = default;
            }

            this.nameToItem.RemoveNode(node);
            this.SetModifiedFlag();
            return true;
        }
        else
        {
            return false;
        }
    }

    private void SetModifiedFlag() => this.ModifiedFlag = true;

    private void ResetModifiedFlag() => this.ModifiedFlag = false;
}
