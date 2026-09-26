// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using Arc.Threading;
using Arc.Unit;
using CrystalData;
using Lp;
using Lp.Data;
using Lp.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using xUnitTest.Lp;

namespace xUnitTest;

[Collection(LpFixtureCollection.Name)]
public sealed class VaultControlTest : IDisposable
{
    private readonly ILogger<VaultControl> logger;
    private readonly string directory = Path.Combine(Path.GetTempPath(), "LpVaultControlTest", Guid.NewGuid().ToString("N"));

    public VaultControlTest(LpFixture fixture)
    {
        this.logger = fixture.ServiceProvider.GetRequiredService<ILogger<VaultControl>>();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(this.directory, true);
        }
        catch
        {
        }
    }

    [Fact]
    public async Task AnExistingVaultIsLoadedEvenIfTheDataDirectoryIsNew()
    {
        var vaultPath = Path.Combine(this.directory, "Keys", "Vault.th"); // The directory is created when saving.
        var first = this.CreateVaultControl(new ScriptedUserInterfaceService("pass", "pass"), vaultPath, "Data1");
        await first.LoadAsync();
        Assert.True(first.NewlyCreated);
        first.Root.AddByteArray("Key", [1, 2, 3]);
        await first.SaveAsync();
        Assert.True(File.Exists(vaultPath));
        Assert.False(File.Exists(vaultPath + ".tmp"));

        // The first run of another data directory uses the same vault (VaultPath).
        var second = this.CreateVaultControl(new ScriptedUserInterfaceService("pass"), vaultPath, "Data2");
        await second.LoadAsync();
        Assert.False(second.NewlyCreated);
        Assert.True(second.Root.TryGetByteArray("Key", out var value, out _));
        Assert.Equal(new byte[] { 1, 2, 3, }, value);
    }

    [Fact]
    public async Task AnUnreadableVaultIsPreservedInsteadOfBeingOverwritten()
    {
        Directory.CreateDirectory(this.directory);
        var vaultPath = Path.Combine(this.directory, "Vault.th");
        PasswordEncryption.Encrypt([0xC1], string.Empty, out var data); // Can be decrypted, but it is not a vault.
        await File.WriteAllBytesAsync(vaultPath, data, TestContext.Current.CancellationToken);

        var ui = new ScriptedUserInterfaceService("pass", "pass");
        var control = this.CreateVaultControl(ui, vaultPath, "Data");
        await control.LoadAsync();
        Assert.True(control.NewlyCreated);
        Assert.False(File.Exists(vaultPath));

        var preserved = Assert.Single(Directory.GetFiles(this.directory));
        Assert.StartsWith(vaultPath + ".", preserved);
        Assert.Equal(data, await File.ReadAllBytesAsync(preserved, TestContext.Current.CancellationToken));
        Assert.Contains(ui.Lines, x => x.Contains(preserved));

        await control.SaveAsync();
        Assert.True(File.Exists(vaultPath));
        Assert.Equal(data, await File.ReadAllBytesAsync(preserved, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AVaultThatCannotBeReadIsLeftAsItIs()
    {
        Directory.CreateDirectory(this.directory);
        var vaultPath = Path.Combine(this.directory, "Vault.th");
        await File.WriteAllBytesAsync(vaultPath, [1, 2, 3], TestContext.Current.CancellationToken);

        var control = this.CreateVaultControl(new ScriptedUserInterfaceService(), vaultPath, "Data");
        using (new FileStream(vaultPath, FileMode.Open, FileAccess.Read, FileShare.Delete))
        {// Reading fails (in use), although the file could be renamed.
            await Assert.ThrowsAsync<PanicException>(control.LoadAsync);
        }

        Assert.Single(Directory.GetFiles(this.directory));
        Assert.Equal(new byte[] { 1, 2, 3, }, await File.ReadAllBytesAsync(vaultPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SavingKeepsTheFileMetadataOfTheVault()
    {
        var vaultPath = Path.Combine(this.directory, "Vault.th");
        var control = this.CreateVaultControl(new ScriptedUserInterfaceService("pass", "pass"), vaultPath, "Data");
        await control.LoadAsync();
        await control.SaveAsync();

        if (OperatingSystem.IsWindows())
        {
            File.SetAttributes(vaultPath, File.GetAttributes(vaultPath) | FileAttributes.Hidden);
            await control.SaveAsync();
            Assert.True(File.GetAttributes(vaultPath).HasFlag(FileAttributes.Hidden));
        }
        else
        {
            File.SetUnixFileMode(vaultPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            await control.SaveAsync();
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(vaultPath));
        }

        Assert.False(File.Exists(vaultPath + ".tmp"));
    }

    [Fact]
    public async Task TerminatingTheNewVaultPromptAbortsInsteadOfUsingAnEmptyPassword()
    {
        var vaultPath = Path.Combine(this.directory, "Vault.th");
        var control = this.CreateVaultControl(new ScriptedUserInterfaceService(), vaultPath, "Data");
        await Assert.ThrowsAsync<PanicException>(control.LoadAsync);
        Assert.False(control.NewlyCreated);
        Assert.False(File.Exists(vaultPath));
    }

    private VaultControl CreateVaultControl(IUserInterfaceService userInterfaceService, string vaultPath, string dataDirectoryName)
    {
        var lpBase = new LpBase();
        lpBase.Initialize(Path.Combine(this.directory, dataDirectoryName), new LpOptions() { VaultPath = vaultPath, }, false, "test");
        return new VaultControl(this.logger, userInterfaceService, lpBase, new CrystalOptions());
    }
}
