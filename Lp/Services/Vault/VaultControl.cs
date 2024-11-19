// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Collections;

namespace Lp.Services;

public partial class VaultControl
{
    public const string Filename = "Vault.tinyhand";

    public VaultControl(ILogger<VaultControl> logger, IUserInterfaceService userInterfaceService, LpBase lpBase, CrystalizerOptions options)
    {// Vault cannot use Crystalizer due to its dependency on IStorageKey.
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.lpBase = lpBase;
        if (!string.IsNullOrEmpty(this.lpBase.Options.VaultPath))
        {
            this.path = this.lpBase.Options.VaultPath;
        }
        else
        {
            this.path = PathHelper.GetRootedFile(this.lpBase.RootDirectory, options.GlobalDirectory.CombineFile(Filename).Path);
        }

        this.Root = new(this);
    }

    #region FieldAndProperty

    public bool NewlyCreated { get; private set; } = false;

    public Vault Root { get; private set; }

    private readonly ILogger logger;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly LpBase lpBase;
    private readonly string path;

    private readonly Lock lockObject = new();
    private readonly OrderedMap<string, DecryptedItem> nameToDecrypted = new();
    private string password = string.Empty;

    #endregion

    [TinyhandObject]
    private partial record struct DecryptedItem
    {
        public DecryptedItem(byte[] decrypted)
        {
            this.Decrypted = decrypted;
        }

        [KeyAsName]
        internal byte[] Decrypted = Array.Empty<byte>();
    }

    [TinyhandObject]
    private partial record struct EncryptedItem
    {
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
        using (this.lockObject.EnterScope())
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

    public bool SerializeAndAdd<T>(string name, T obj)
    {
        var bytes = TinyhandSerializer.Serialize<T>(obj);
        return this.Add(name, bytes);
    }

    public bool Add(string name, byte[] decrypted)
    {
        using (this.lockObject.EnterScope())
        {
            if (this.nameToDecrypted.TryGetValue(name, out var item))
            {
                this.nameToDecrypted.Remove(name);
            }

            this.nameToDecrypted.Add(name, new(decrypted));
            return true;
        }
    }

    public bool Exists(string name)
    {
        using (this.lockObject.EnterScope())
        {
            return this.nameToDecrypted.ContainsKey(name);
        }
    }

    public bool Remove(string name)
    {
        using (this.lockObject.EnterScope())
        {
            return this.nameToDecrypted.Remove(name);
        }
    }

    public bool TryGet(string name, [MaybeNullWhen(false)] out byte[] decrypted)
    {
        using (this.lockObject.EnterScope())
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

    public bool TryGetAndParse<T>(string name, [MaybeNullWhen(false)] out T obj)
        where T : IStringConvertible<T>
    {
        if (!this.TryGet(name, out var decrypted))
        {
            obj = default;
            return false;
        }

        return T.TryParse(System.Text.Encoding.UTF8.GetString(decrypted), out obj);
    }

    public string[] GetNames()
    {
        using (this.lockObject.EnterScope())
        {
            return this.nameToDecrypted.Select(x => x.Key).ToArray();
        }
    }

    public string[] GetNames(string prefix)
    {
        using (this.lockObject.EnterScope())
        {
            (var lower, var upper) = this.nameToDecrypted.GetRange(prefix, prefix + "\uffff");
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

    public async Task SaveAsync()
    {
        try
        {
            await File.WriteAllBytesAsync(this.path, this.Root.SerializeVault()).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    internal async Task LoadAsync()
    {
        if (this.lpBase.IsFirstRun)
        {// First run
        }
        else
        {
            var result = await this.LoadAsync(this.lpBase.Options.VaultPass).ConfigureAwait(false);
            if (result)
            {
                return;
            }

            // Could not load Vault
            var reply = await this.userInterfaceService.RequestYesOrNo(Hashed.Vault.AskNew);
            if (reply != true)
            {// No
                throw new PanicException();
            }
        }

        this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Vault.Create));
        // await this.UserInterfaceService.Notify(UserInterfaceNotifyLevel.Information, Hashed.KeyVault.Create);

        // New Vault
        var password = this.lpBase.Options.VaultPass;
        if (string.IsNullOrEmpty(password))
        {
            password = await this.userInterfaceService.RequestPasswordAndConfirm(Hashed.Vault.EnterPassword, Hashed.Dialog.Password.Confirm);
        }

        if (password == null)
        {
            throw new PanicException();
        }

        this.NewlyCreated = true;
        this.Root.SetPassword(password);
    }

    private async Task<bool> LoadAsync(string? lppass)
    {
        byte[] data;
        try
        {
            data = await File.ReadAllBytesAsync(this.path).ConfigureAwait(false);
        }
        catch
        {
            this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.Load, this.path);
            return false;
        }

        if (PasswordEncryption.TryDecrypt(data, string.Empty, out var plaintext))
        {// No password
            if (TinyhandSerializer.TryDeserializeObject<Vault>(plaintext, out var vault))
            {// Success
                this.Root = vault;
                this.Root.SetPassword(string.Empty);
                return true;
            }
            else
            {// Deserialize failed
                this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.Deserialize, this.path);
                return false;
            }
        }

        string? password = lppass;
        while (true)
        {
            if (password == null)
            {// Enter password
                password = await this.userInterfaceService.RequestPassword(Hashed.Vault.EnterPassword).ConfigureAwait(false);
                if (password == null)
                {
                    throw new PanicException();
                }
            }

            if (PasswordEncryption.TryDecrypt(data, password, out plaintext))
            {// Success
                if (TinyhandSerializer.TryDeserializeObject<Vault>(plaintext, out var vault))
                {// Success
                    this.Root = vault;
                    this.Root.SetPassword(password);
                    return true;
                }
                else
                {// Deserialize failed
                    this.logger.TryGet(LogLevel.Error)?.Log(Hashed.Error.Deserialize, this.path);
                    return false;
                }
            }
            else
            {// Failure
                password = null;
                await this.userInterfaceService.Notify(LogLevel.Warning, Hashed.Dialog.Password.NotMatch).ConfigureAwait(false);
            }
        }
    }
}
