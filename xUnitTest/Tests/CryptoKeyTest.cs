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
        cryptoKey.SubId.Is(0u);
        cryptoKey.ValidateSubId().IsTrue();
        cryptoKey.TryGetPublicKey(out var publicKey).IsTrue();
        publicKey.Is(originalPublicKey);

        cryptoKey = new CryptoKey(ref originalPublicKey, true);
        cryptoKey.SubId.IsNot(0u);
        cryptoKey.ValidateSubId().IsTrue();
        cryptoKey.TryGetPublicKey(out publicKey).IsTrue();
        publicKey.Is(originalPublicKey);

        var encryptionPublicKey = mergerKey.GetEncryptionPublicKey();

        cryptoKey = new CryptoKey(originalKey, ref encryptionPublicKey, false);
        cryptoKey.SubId.Is(0u);
        cryptoKey.ValidateSubId().IsTrue();
        cryptoKey.TryGetPublicKey(out publicKey).IsFalse();
        cryptoKey.TryDecrypt(mergerKey).IsTrue();
        cryptoKey.TryGetPublicKey(out publicKey).IsTrue();
        publicKey.Is(originalPublicKey);

        cryptoKey.ClearDecrypted();
        cryptoKey.TryDecrypt(originalKey, ref mergerPublicKey).IsTrue();
        cryptoKey.TryGetPublicKey(out publicKey).IsTrue();
        publicKey.Is(originalPublicKey);
    }
}
