// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc;
using Lp.Services;
using Netsphere;
using Netsphere.Crypto;
using Xunit;

namespace xUnitTest;

public class AliasTest
{
    public const string AliasName = "test";
    public const string AliasName2 = "test2";

    [Fact]
    public void Test1()
    {
        Alias.IsValid(string.Empty).IsFalse();
        Alias.IsValid("test").IsTrue();
        Alias.IsValid("0test").IsFalse();
        Alias.IsValid("test1").IsTrue();
        Alias.IsValid("test1_2").IsTrue();
    }

    [Fact]
    public void TestSignaturePublicKey()
    {
        var seedKey = SeedKey.NewSignature();
        var publicKey = seedKey.GetSignaturePublicKey();
        SignaturePublicKey publicKey2;
        int read;

        Alias.Add(AliasName, publicKey);
        Alias.TryGetPublicKeyFromAlias(AliasName, out publicKey2).IsTrue();
        publicKey.Equals(publicKey2).IsTrue();

        SignaturePublicKey.TryParse(AliasName2, out publicKey2, out read).IsFalse();
        SignaturePublicKey.TryParse(AliasName, out publicKey2, out read).IsTrue();
        publicKey2.ToString().Is(AliasName);
        publicKey.Equals(publicKey2).IsTrue();
    }

    [Fact]
    public void TestIdentifier()
    {
        var publicKey = SeedKey.NewSignature().GetSignaturePublicKey();
        var identifier = publicKey.GetIdentifier();
        Identifier identifier2;
        int read;

        Alias.Add(AliasName, identifier);
        Alias.TryGetIdentifierFromAlias(AliasName, out identifier2).IsTrue();
        identifier.Equals(identifier2).IsTrue();

        Identifier.TryParse(AliasName2, out identifier2, out read).IsFalse();
        Identifier.TryParse(AliasName, out identifier2, out read).IsTrue();
        identifier2.ToString().Is(AliasName);
        identifier.Equals(identifier2).IsTrue();
    }
}
