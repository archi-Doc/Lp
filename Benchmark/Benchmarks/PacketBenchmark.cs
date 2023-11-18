// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;
using Arc.Crypto;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class PacketBenchmark
{
    public const int Length = 1008;

    public Aes Aes { get; }

    public Sha2_256 Sha256 { get; }

    public byte[] Key { get; }

    public byte[] Iv { get; }

    public byte[] Source { get; }

    public byte[] Encrypted { get; }

    public byte[] Decrypted { get; }

    public Memory<byte> Encrypted_PKCS7 { get; }

    public int Pkcs7Length { get; }

    public PacketBenchmark()
    {
        this.Aes = Aes.Create();
        this.Sha256 = new();

        this.Key = new byte[16];
        RandomVault.Pseudo.NextBytes(this.Key);
        this.Aes.Key = this.Key;
        this.Iv = new byte[16];
        RandomVault.Pseudo.NextBytes(this.Iv);

        this.Source = new byte[Length];
        this.Encrypted = new byte[Length];
        this.Aes.TryEncryptCbc(this.Source, this.Iv, this.Encrypted, out var written, PaddingMode.None);
        this.Decrypted = new byte[Length];
        this.Aes.TryDecryptCbc(this.Encrypted, this.Iv, this.Decrypted, out written, PaddingMode.None);

        this.Encrypted_PKCS7 = new byte[Length + 32];
        this.Aes.TryEncryptCbc(this.Source, this.Iv, this.Encrypted_PKCS7.Span, out written, PaddingMode.PKCS7);
        this.Encrypted_PKCS7 = this.Encrypted_PKCS7.Slice(0, written);
        this.Aes.TryDecryptCbc(this.Encrypted_PKCS7.Span, this.Iv, this.Decrypted, out written, PaddingMode.PKCS7);
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public ulong GetXxHash3()
    {
        return XxHash3.Hash64(this.Source);
    }

    [Benchmark]
    public byte[] Decrypt()
    {
        this.Aes.TryDecryptCbc(this.Encrypted, this.Iv, this.Decrypted, out var written, PaddingMode.None);
        return this.Decrypted;
    }

    [Benchmark]
    public byte[] Decrypt_PKCS7()
    {
        this.Aes.TryDecryptCbc(this.Encrypted_PKCS7.Span, this.Iv, this.Decrypted, out var written, PaddingMode.PKCS7);
        return this.Decrypted;
    }

    [Benchmark]
    public byte[] Sha256AndDecrypt()
    {
        Span<byte> source = stackalloc byte[32];
        this.Encrypted.AsSpan(0, 16).CopyTo(source);
        this.Iv.AsSpan().CopyTo(source.Slice(16));

        Span<byte> hash = stackalloc byte[32];
        Sha3Helper.Get256_Span(source, hash);
        this.Aes.TryDecryptCbc(this.Encrypted, hash.Slice(0, 16), this.Decrypted, out var written, PaddingMode.None);
        return this.Decrypted;
    }
}
