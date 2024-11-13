// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using Netsphere.Crypto;
using Tinyhand;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class SignatureBenchmark
{
    private readonly EncryptionPrivateKey privateKeyA;
    private readonly EncryptionPrivateKey privateKeyB;
    private readonly EncryptionPublicKey publicKeyA;
    private readonly EncryptionPublicKey publicKeyB;

    public SignatureBenchmark()
    {
        this.privateKeyA = EncryptionPrivateKey.Create();
        this.privateKeyB = EncryptionPrivateKey.Create();
        this.publicKeyA = this.privateKeyA.ToPublicKey();
        this.publicKeyB = this.privateKeyB.ToPublicKey();
    }

    [Benchmark]
    public EncryptionPrivateKey CreateKey()
    {
        return EncryptionPrivateKey.Create();
    }

    [Benchmark]
    public byte[] DeriveKeyMaterial()
    {
        var x = new byte[32];
        this.publicKeyB.WriteX(x);
        var ecdh = KeyHelper.CreateEcdhFromX(x, this.publicKeyB.YTilde)!;
        var ecdh2 = this.privateKeyA.TryGetEcdh()!;
        var b = ecdh2.DeriveKeyMaterial(ecdh.PublicKey);
        return b;
    }

    /*[Benchmark]
    public Identifier GetIdentifier2()
    {
        var identifier = Hash.GetIdentifier(this.Class, 0x40000000);
        return identifier;
    }*/
}
