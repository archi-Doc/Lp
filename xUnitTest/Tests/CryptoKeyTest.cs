// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using Xunit;

namespace xUnitTest;

public class CryptoKeyTest
{
    [Fact]
    public void Test1()
    {
        var originalKey = SeedKey.NewSignature();
        var mergerKey = SeedKey.NewEncryption();
        var originalPublicKey = originalKey.GetSignaturePublicKey();
        var mergerPublicKey = mergerKey.GetEncryptionPublicKey();

        var cryptoKey = new CryptoKey(ref originalPublicKey, false);
        cryptoKey.SubKey.Is(0u);
        cryptoKey.ValidateSubKey().IsTrue();
        cryptoKey.TryGetPublicKey(out var publicKey).IsTrue();
        publicKey.Is(originalPublicKey);

        cryptoKey = new CryptoKey(ref originalPublicKey, true);
        cryptoKey.SubKey.IsNot(0u);
        cryptoKey.ValidateSubKey().IsTrue();
        cryptoKey.TryGetPublicKey(out publicKey).IsTrue();
        publicKey.Is(originalPublicKey);

        var encryptionPublicKey = mergerKey.GetEncryptionPublicKey();

        cryptoKey = new CryptoKey(originalKey, ref encryptionPublicKey, false);
        cryptoKey.SubKey.Is(0u);
        cryptoKey.ValidateSubKey().IsTrue();
        cryptoKey.TryGetPublicKey(out publicKey).IsFalse();
        cryptoKey.TryDecrypt(mergerKey).IsTrue();
        cryptoKey.TryGetPublicKey(out publicKey).IsTrue();
        publicKey.Is(originalPublicKey);

        cryptoKey.ClearDecrypted();
        cryptoKey.TryDecrypt(originalKey, ref mergerPublicKey).IsTrue();
        cryptoKey.TryGetPublicKey(out publicKey).IsTrue();
        publicKey.Is(originalPublicKey);
    }

    [Fact]
    public void TestString()
    {
        var originalKey = SeedKey.NewSignature();
        var mergerKey = SeedKey.NewEncryption();
        var originalPublicKey = originalKey.GetSignaturePublicKey();
        var mergerPublicKey = mergerKey.GetEncryptionPublicKey();

        var cryptoKey = new CryptoKey(ref originalPublicKey, false);
        var st = cryptoKey.ToString();
        CryptoKey.TryParse(st, out var cryptoKey2, out _).IsTrue();
        cryptoKey2!.Equals(cryptoKey).IsTrue();

        cryptoKey = new CryptoKey(ref originalPublicKey, true);
        st = cryptoKey.ToString();
        CryptoKey.TryParse(st, out cryptoKey2, out _).IsTrue();
        cryptoKey2!.Equals(cryptoKey).IsTrue();

        cryptoKey = new CryptoKey(originalKey, ref mergerPublicKey, false);
        st = cryptoKey.ToString();
        CryptoKey.TryParse(st, out cryptoKey2, out _).IsTrue();
        cryptoKey2!.Equals(cryptoKey).IsTrue();

        cryptoKey = new CryptoKey(originalKey, ref mergerPublicKey, true);
        st = cryptoKey.ToString();
        CryptoKey.TryParse(st, out cryptoKey2, out _).IsTrue();
        cryptoKey2!.Equals(cryptoKey).IsTrue();
    }

    [Fact]
    public void TestInvalidString()
    {
        CryptoKey.TryParse("(3570670142!_X-juYNIBmIlFtzLi9zo5ij_JBGYFnAYsWASWh0FyVf8a1Im)", out var cryptoKey, out _).IsTrue();
        CryptoKey.TryParse("(3570670143!_X-juYNIBmIlFtzLi9zo5ij_JBGYFnAYsWASWh0FyVf8a1Im)", out cryptoKey, out _).IsFalse();
    }
}
