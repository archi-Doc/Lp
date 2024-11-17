// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security;
using Arc.Collections;

namespace Lp.Services;

[TinyhandObject(UseServiceProvider = true, LockObject = nameof(lockObject))]
public sealed partial class Vault : ITinyhandSerializationCallback
{
    [TinyhandObject]
    private readonly partial struct Item
    {// Plaintext, Plaintext+Object, Ciphertext, Ciphertext+Vault
        public Item(byte[]? byteArray, ITinyhandSerialize? @object)
        {
            this.ByteArray = byteArray;
            this.Object = @object;
        }

        public Item(byte[] data)
        {
            this.ByteArray = data;
            this.Object = default;
        }

        public Item(ITinyhandSerialize? @object)
        {
            this.ByteArray = default;
            this.Object = @object;
        }

        public Item Clone(byte[] data)
            => new(data, this.Object);

        public Item Clone(ITinyhandSerialize @object)
            => new(this.ByteArray, @object);

        [Key(0)]
        public readonly byte[]? ByteArray;

        [IgnoreMember]
        public readonly ITinyhandSerialize? Object;
    }

    #region FieldAndProperty

    private readonly VaultControl vaultControl;
    private readonly Lock lockObject = new();

    [IgnoreMember]
    public bool ModifiedFlag { get; private set; }

    [Key(0)]
    private OrderedMap<string, Item> nameToItem = new();

    [IgnoreMember]
    private string password = string.Empty;

    #endregion

    public Vault(VaultControl vaultControl)
    {
        this.vaultControl = vaultControl;
    }

    public VaultResult TryAddByteArray(string name, byte[] byteArray)
    {
        using (this.lockObject.EnterScope())
        {
            if (this.nameToItem.TryGetValue(name, out var item))
            {// Already exists.
                return VaultResult.AlreadyExists;
            }

            this.nameToItem.Add(name, new(byteArray));
            this.SetModifiedFlag();
            return VaultResult.Success;
        }
    }

    public void AddByteArray(string name, byte[] plaintext)
    {
        using (this.lockObject.EnterScope())
        {
            this.nameToItem.TryGetValue(name, out var item);
            this.nameToItem.Add(name, new(plaintext));
            this.SetModifiedFlag();
        }
    }

    public VaultResult TryAddObject(string name, ITinyhandSerialize @object)
    {
        using (this.lockObject.EnterScope())
        {
            if (this.nameToItem.TryGetValue(name, out var item))
            {// Already exists.
                return VaultResult.AlreadyExists;
            }

            this.nameToItem.Add(name, new(@object));
            this.SetModifiedFlag();
            return VaultResult.Success;
        }
    }

    public void AddObject(string name, ITinyhandSerialize @object)
    {
        using (this.lockObject.EnterScope())
        {
            this.nameToItem.TryGetValue(name, out var item);
            this.nameToItem.Add(name, new(@object));
            this.SetModifiedFlag();
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

    public bool TryGetByteArray(string name, [MaybeNullWhen(false)] out byte[] byteArray)
    {
        using (this.lockObject.EnterScope())
        {
            if (!this.nameToItem.TryGetValue(name, out var item))
            {// Not found
                byteArray = null;
                return false;
            }

            byteArray = item.ByteArray;
            return byteArray is not null;
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

    public void Create(string password)
    {
        using (this.lockObject.EnterScope())
        {
            //this.Created = true;
            this.password = password;
            this.nameToItem.Clear();
        }
    }

    public bool IsPasswordEqual(string password)
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

    void ITinyhandSerializationCallback.OnAfterReconstruct()
    {
    }

    void ITinyhandSerializationCallback.OnAfterDeserialize()
    {
    }

    void ITinyhandSerializationCallback.OnBeforeSerialize()
    {
        foreach (var x in this.nameToItem)
        {
        }
    }

    private void SetModifiedFlag() => this.ModifiedFlag = true;

    private void ClearModifiedFlag() => this.ModifiedFlag = false;
}
