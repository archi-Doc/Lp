// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Crypto;
using Lp.Services;
using Microsoft.Extensions.DependencyInjection;
using Tinyhand;
using Xunit;
using xUnitTest.Lp;

namespace xUnitTest;

[Collection(LpFixtureCollection.Name)]
public class VaultResultTest
{
    private readonly Vault root;
    private readonly string prefix = Guid.NewGuid().ToString("N") + "\\";

    public VaultResultTest(LpFixture fixture)
    {
        this.root = fixture.ServiceProvider.GetRequiredService<VaultControl>().Root;
    }

    [Fact]
    public void UndeserializableContentIsReportedAsAFailureNotASuccess()
    {
        var name = this.prefix + "broken";
        this.root.AddByteArray(name, [0xC1]); // Never a valid MessagePack value.

        Assert.False(this.root.TryGet<VaultTestRecord>(name, out var value, out var result));
        Assert.Null(value);
        Assert.Equal(VaultResult.DeserializationFailure, result);
    }

    [Fact]
    public void ARoundTrippedValueIsReportedAsASuccess()
    {
        var name = this.prefix + "record";
        var record = new VaultTestRecord(7, "seven");
        Assert.True(this.root.TryAdd(name, record, out var addResult));
        Assert.Equal(VaultResult.Success, addResult);
        Assert.False(this.root.TryAdd(name, record, out addResult));
        Assert.Equal(VaultResult.AlreadyExists, addResult);

        Assert.True(this.root.TryGet<VaultTestRecord>(name, out var value, out var result));
        Assert.Equal(VaultResult.Success, result);
        Assert.Equal(record, value);
    }

    [Fact]
    public void MissingNamesAreReportedAsNotFound()
    {
        var name = this.prefix + "missing";
        Assert.False(this.root.TryGet<VaultTestRecord>(name, out _, out var result));
        Assert.Equal(VaultResult.NotFound, result);
        Assert.False(this.root.TryGetByteArray(name, out _, out result));
        Assert.Equal(VaultResult.NotFound, result);
        Assert.False(this.root.TryGetObject<VaultTestRecord>(name, out _, out result));
        Assert.Equal(VaultResult.NotFound, result);
        Assert.False(this.root.TryGetVault(name, null, out _, out result));
        Assert.Equal(VaultResult.NotFound, result);
        Assert.False(this.root.Contains(name));
        Assert.False(this.root.Remove(name));
    }

    [Fact]
    public void NamesArePrefixFilteredAndRemovable()
    {
        this.root.AddByteArray(this.prefix + "a", [1]);
        this.root.AddByteArray(this.prefix + "b", [2]);

        var names = this.root.GetNames(this.prefix);
        Assert.Equal([this.prefix + "a", this.prefix + "b"], names.Order());
        Assert.Contains(this.prefix + "a", this.root.GetNames());
        Assert.Empty(this.root.GetNames(Guid.NewGuid().ToString("N")));

        Assert.True(this.root.Remove(this.prefix + "a"));
        Assert.Equal([this.prefix + "b"], this.root.GetNames(this.prefix));
    }

    [Fact]
    public void AChildVaultTracksItsParentAndPassword()
    {
        var name = this.prefix + "child";
        Assert.True(this.root.TryAddVault(name, out var child, out var result));
        Assert.Equal(VaultResult.Success, result);
        Assert.Same(this.root, child.ParentVault);
        Assert.False(this.root.TryAddVault(name, out _, out result));
        Assert.Equal(VaultResult.AlreadyExists, result);

        child.SetPassword("pass");
        Assert.True(child.PasswordEquals("pass"));
        Assert.False(child.PasswordEquals("other"));

        Assert.True(this.root.TryGetVault(name, null, out var loaded, out result));
        Assert.Same(child, loaded);
        Assert.True(this.root.TryGetVault(name, "pass", out loaded, out result));
        Assert.False(this.root.TryGetVault(name, "wrong", out _, out result));
        Assert.Equal(VaultResult.PasswordMismatch, result);

        Assert.True(this.root.Remove(name));
        Assert.Null(child.ParentVault);
    }

    [Fact]
    public void TheModifiedFlagIsRaisedByEveryMutation()
    {
        var vaultControl = new Vault(null!);
        Assert.False(vaultControl.ModifiedFlag);
        vaultControl.AddByteArray(this.prefix + "x", [1]);
        Assert.True(vaultControl.ModifiedFlag);
    }

    [Fact]
    public void AnEncryptedChildVaultIsRestoredWithItsPassword()
    {
        var name = this.prefix + "encrypted";
        this.root.AddVault(name, out var child);
        child.SetPassword("secret");
        child.AddByteArray("inner", [9, 8, 7]);

        var copy = TinyhandSerializer.Deserialize<Vault>(TinyhandSerializer.Serialize(this.root))!;
        Assert.False(copy.TryGetVault(name, "wrong", out _, out var result));
        Assert.Equal(VaultResult.PasswordMismatch, result);

        Assert.True(copy.TryGetVault(name, "secret", out var restored, out result));
        Assert.True(restored.TryGetByteArray("inner", out var bytes, out result));
        Assert.Equal([9, 8, 7], bytes);
    }

    [Fact]
    public void GettingAVaultFromAnotherKindOfItemIsAKindMismatch()
    {// Not a password mismatch, which would make the caller ask for a password again and again.
        var name = this.prefix + "bytes";
        this.root.AddByteArray(name, [1, 2, 3]);

        Assert.False(this.root.TryGetVault(name, null, out _, out var result));
        Assert.Equal(VaultResult.KindMismatch, result);
        Assert.False(this.root.TryGetVault(name, "pass", out _, out result));
        Assert.Equal(VaultResult.KindMismatch, result);

        Assert.True(this.root.Remove(name));
    }

    [Fact]
    public void AnItemIsKeptWhenItsObjectCannotBeSerializedAgain()
    {
        var vault = new Vault(null!);
        var obj = new ThrowingVaultObject() { Value = 42, };
        vault.AddObject("item", obj);
        _ = vault.SerializeVault(); // The item now holds its serialized data.

        obj.ThrowOnSerialize = true;
        var data = vault.SerializeVault();
        obj.ThrowOnSerialize = false;

        Assert.True(vault.Contains("item"));
        Assert.True(PasswordEncryption.TryDecrypt(data, string.Empty, out var plaintext));
        Assert.True(TinyhandSerializer.TryDeserializeObject<Vault>(plaintext, out var restored));
        Assert.True(restored.TryGetObject<ThrowingVaultObject>("item", out var restoredObject, out _));
        Assert.Equal(42, restoredObject.Value); // The previously serialized data is saved.
    }
}

[TinyhandObject]
public partial class ThrowingVaultObject
{
    private int value;

    [Key(0)]
    public int Value
    {
        get => this.ThrowOnSerialize ? throw new InvalidOperationException() : this.value;
        set => this.value = value;
    }

    [IgnoreMember]
    public bool ThrowOnSerialize { get; set; }
}
