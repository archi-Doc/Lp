// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Lp.Services;
using Microsoft.Extensions.DependencyInjection;
using Tinyhand;
using Xunit;

namespace xUnitTest.Lp;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record VaultTestRecord(int Id, string Name);

[Collection(LpFixtureCollection.Name)]
public class VaultTest
{
    private readonly LpFixture fixture;
    private readonly IServiceProvider serviceProvider;

    private readonly string byteArrayKey = "ByteArrayKey";
    private readonly byte[] byteArrayValue = [1, 2, 3, 4, 5];
    private readonly string objectKey = "ObjectKey";
    private readonly string vaultKey = "VaultKey";

    public VaultTest(LpFixture fixture)
    {
        this.fixture = fixture;
        this.serviceProvider = this.fixture.ServiceProvider;
    }

    [Fact]
    public void Test1()
    {
        var vaultControl = this.serviceProvider.GetRequiredService<VaultControl>();
        var root = vaultControl.Root;
        VaultResult result;

        // Add values
        root.AddByteArray(this.byteArrayKey, this.byteArrayValue);
        var record = new VaultTestRecord(1, "Test");
        root.AddObject(this.objectKey, record);

        // Child vault
        root.AddVault(this.vaultKey, out var child);
        child.SetPassword("1");
        child.AddByteArray(this.byteArrayKey, this.byteArrayValue);
        child.AddObject(this.objectKey, record);

        // ByteArray
        root.TryAddByteArray(this.byteArrayKey, this.byteArrayValue, out result).IsFalse();
        result.Is(VaultResult.AlreadyExists);
        root.TryGetByteArray(this.byteArrayKey, out var byteArrayValue2, out result).IsTrue();
        result.Is(VaultResult.Success);
        byteArrayValue2!.SequenceEqual(this.byteArrayValue).IsTrue();
        root.TryGetByteArray(this.objectKey, out _, out result).IsFalse();
        result.Is(VaultResult.KindMismatch);
        root.TryGetByteArray(this.vaultKey, out _, out result).IsFalse();
        result.Is(VaultResult.KindMismatch);

        // Object
        root.TryAddObject(this.objectKey, record, out result).IsFalse();
        result.Is(VaultResult.AlreadyExists);
        root.TryGetObject<VaultTestRecord>(this.objectKey, out var record2, out result).IsTrue();
        result.Is(VaultResult.Success);
        record2!.Equals(record).IsTrue();
        root.TryGetObject<VaultTestRecord>(this.byteArrayKey, out _, out result).IsFalse();
        result.Is(VaultResult.KindMismatch);
        root.TryGetObject<VaultTestRecord>(this.vaultKey, out _, out result).IsFalse();
        result.Is(VaultResult.KindMismatch);

        // Child vault
        child.TryGetByteArray(this.byteArrayKey, out byteArrayValue2, out result).IsTrue();
        result.Is(VaultResult.Success);
        byteArrayValue2!.SequenceEqual(this.byteArrayValue).IsTrue();
        child.TryGetObject<VaultTestRecord>(this.objectKey, out record2, out result).IsTrue();
        result.Is(VaultResult.Success);
        record2!.Equals(record).IsTrue();

        // Serialize and deserialize
        var b = TinyhandSerializer.Serialize(root);
        root = TinyhandSerializer.Deserialize<Vault>(b)!;

        root.TryGetByteArray(this.byteArrayKey, out byteArrayValue2, out result).IsTrue();
        result.Is(VaultResult.Success);
        byteArrayValue2!.SequenceEqual(this.byteArrayValue).IsTrue();
        root.TryGetObject<VaultTestRecord>(this.objectKey, out record2, out result).IsTrue();
        result.Is(VaultResult.Success);
        record2!.Equals(record).IsTrue();

        // Child vault
        root.TryGetVault(this.vaultKey, "2", out var child2).IsFalse();
        root.TryGetVault(this.vaultKey, "1", out child2!).IsTrue();
        child2.TryGetByteArray(this.byteArrayKey, out byteArrayValue2, out result).IsTrue();
        result.Is(VaultResult.Success);
        byteArrayValue2!.SequenceEqual(this.byteArrayValue).IsTrue();
        child2.TryGetObject<VaultTestRecord>(this.objectKey, out record2, out result).IsTrue();
        result.Is(VaultResult.Success);
        record2!.Equals(record).IsTrue();
    }
}
