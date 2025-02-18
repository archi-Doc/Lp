// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using Xunit;

namespace xUnitTest.Lp;

public class CryptoTest
{
    /*[Fact]
    public void TestCryptoKey()
    {
        var privateKey = SignaturePrivateKey.Create();
        var publicKey = privateKey.ToPublicKey();

        // CryptoKey (Raw)
        var cryptoKey = CryptoKey.CreateRaw(publicKey);
        cryptoKey.TryGetRawKey(out var originalKey).IsTrue();
        originalKey.Equals(publicKey).IsTrue();

        CryptoKey.TryParse(cryptoKey.ToString(), out var cryptoKey2).IsTrue();
        cryptoKey.Equals(cryptoKey2).IsTrue();

        // CryptoKey (Encrypted)
        var mergerKey = EncryptionPrivateKey.Create();
        CryptoKey.TryCreateEncrypted(privateKey, mergerKey.ToPublicKey(), 0, out cryptoKey).IsTrue();
        cryptoKey!.IsOriginalKey(privateKey).IsTrue();

        cryptoKey.TryGetEncryptedKey(mergerKey, out originalKey).IsTrue();
        publicKey.Equals(originalKey).IsTrue();

        CryptoKey.TryParse(cryptoKey.ToString(), out cryptoKey2).IsTrue();
        cryptoKey.Equals(cryptoKey2).IsTrue();
    }*/
}
