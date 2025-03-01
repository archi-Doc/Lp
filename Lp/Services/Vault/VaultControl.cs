// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
            this.path = PathHelper.GetRootedFile(this.lpBase.DataDirectory, options.GlobalDirectory.CombineFile(Filename).Path);
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

    #endregion

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
            var result = await this.ReadAndDecrypt(this.lpBase.Options.VaultPass).ConfigureAwait(false);
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

    private async Task<bool> ReadAndDecrypt(string? lppass)
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
