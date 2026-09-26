// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Globalization;
using CrystalData;

namespace Lp.Services;

public partial class VaultControl
{
    public const string Filename = "Vault" + CrystalControl.BinaryExtension;
    private const string TemporaryExtension = ".tmp";

    public VaultControl(ILogger<VaultControl> logger, IUserInterfaceService userInterfaceService, LpBase lpBase, CrystalOptions options)
    {// Vault cannot use CrystalControl due to its dependency on IStorageKey.
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
            var data = this.Root.SerializeVault();
            if (Path.GetDirectoryName(this.path) is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
            }

            // Write to a temporary file and then replace the vault, so that an interrupted write cannot corrupt the only copy.
            var exists = File.Exists(this.path);
            var temporaryPath = this.path + TemporaryExtension;
            File.Delete(temporaryPath); // A leftover file would keep its own permissions.
            var options = new FileStreamOptions() { Mode = FileMode.CreateNew, Access = FileAccess.Write, Options = FileOptions.Asynchronous | FileOptions.WriteThrough, }; // WriteThrough: the data reaches the disk before the vault is replaced.
            if (exists && !OperatingSystem.IsWindows())
            {// Keep the permissions of the vault (e.g., 600).
                options.UnixCreateMode = File.GetUnixFileMode(this.path);
            }

            using (var stream = new FileStream(temporaryPath, options))
            {
                await stream.WriteAsync(data).ConfigureAwait(false);
            }

            if (exists && OperatingSystem.IsWindows())
            {// Keep the ACL and attributes of the vault.
                File.Replace(temporaryPath, this.path, null);
            }
            else
            {
                File.Move(temporaryPath, this.path, true);
            }
        }
        catch (Exception e)
        {
            this.logger.GetWriter(LogLevel.Error)?.Write(Hashed.Error.Save, this.path, e.Message);
        }
    }

    internal async Task LoadAsync()
    {
        if (File.Exists(this.path))
        {// The vault may exist outside the data directory (VaultPath), so check the file itself.
            var result = await this.ReadAndDecrypt(this.lpBase.Options.VaultPass).ConfigureAwait(false);
            if (result)
            {
                return;
            }

            // Could not load Vault
            /*var reply = await this.userInterfaceService.RequestYesOrNo(Hashed.Vault.AskNew);
            if (reply != true)
            {// No
                throw new PanicException();
            }*/

            // The vault was read and decrypted but could not be deserialized: keep it instead of overwriting it with a new one when saving.
            var preservedPath = $"{this.path}.{DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}";
            try
            {
                File.Move(this.path, preservedPath);
            }
            catch
            {
                throw new PanicException();
            }

            this.userInterfaceService.WriteLineWarning(Hashed.Vault.Preserved, preservedPath);
        }

        this.userInterfaceService.WriteLine(HashedString.Get(Hashed.Vault.Create));
        // await this.UserInterfaceService.Notify(UserInterfaceNotifyLevel.Information, Hashed.KeyVault.Create);

        // New Vault
        var password = this.lpBase.Options.VaultPass;
        if (string.IsNullOrEmpty(password))
        {
            var result = await this.userInterfaceService.ReadPasswordAndConfirm(false, Hashed.Vault.EnterPassword, Hashed.Dialog.Password.Confirm);
            if (result.IsTerminated)
            {// Abort as ReadAndDecrypt() does, instead of continuing and saving a vault with an empty password.
                throw new PanicException();
            }
            else if (result.IsSuccess)
            {
                password = result.Text;
            }
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
        {// The vault exists but cannot be read (e.g., access denied or in use), which does not mean it is broken: stop without replacing it.
            this.logger.GetWriter(LogLevel.Error)?.Write(Hashed.Error.Load, this.path);
            throw new PanicException();
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
                this.logger.GetWriter(LogLevel.Error)?.Write(Hashed.Error.Deserialize, this.path);
                return false;
            }
        }

        string? password = string.IsNullOrEmpty(lppass) ? null : lppass; // The empty password has already been tried.
        while (true)
        {
            if (password == null)
            {// Enter password
                var inputResult = await this.userInterfaceService.ReadPassword(false, Hashed.Vault.EnterPassword).ConfigureAwait(false);
                if (!inputResult.IsSuccess)
                {// Terminated
                    throw new PanicException();
                }

                password = inputResult.Text;
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
                    this.logger.GetWriter(LogLevel.Error)?.Write(Hashed.Error.Deserialize, this.path);
                    return false;
                }
            }
            else
            {// Failure
                password = null;
                this.userInterfaceService.WriteLineWarning(Hashed.Dialog.Password.NotMatch);
            }
        }
    }
}
