// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BenchmarkDotNet.Attributes;
using Lp;
using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;
using Tinyhand;
using LpCredit = Lp.T3cs.Credit;

namespace Benchmark;

[MemoryDiagnoser]
public class LpAllocationBenchmark
{
    private readonly ushort[] indices = Enumerable.Range(0, 23).Select(x => (ushort)x).ToArray();
    private string phrase = string.Empty;
    private Authority authority = default!;
    private LpCredit credit = default!;
    private SignaturePublicKey[] mergers = [];
    private PeerProof proof = default!;
    private PeerProof proofCopy = default!;
    private Vault vault = default!;

    [GlobalSetup]
    public void Setup()
    {
        this.phrase = Seedphrase.Create(this.indices);
        this.authority = new Authority(new byte[32], AuthorityLifecycle.Application, 0);
        this.credit = new LpCredit(default, [this.authority.GetSignaturePublicKey()]);
        _ = this.authority.GetSeedKey(this.credit);

        this.mergers = [SeedKey.NewSignature().GetSignaturePublicKey(), SeedKey.NewSignature().GetSignaturePublicKey()];

        var seedKey = SeedKey.NewSignature();
        this.proof = new PeerProof(seedKey.GetSignaturePublicKey());
        seedKey.TrySign(this.proof, 60);
        this.proofCopy = TinyhandSerializer.Deserialize<PeerProof>(TinyhandSerializer.Serialize(this.proof))!;

        this.vault = new Vault(default!);
        for (var i = 0; i < 16; i++)
        {
            this.vault.AddByteArray($"Authority/name{i}", [1]);
        }
    }

    [Benchmark]
    public byte[]? ParseSeedphrase() => Seedphrase.TryGetSeed(this.phrase);

    [Benchmark]
    public string CreateSeedphrase() => Seedphrase.Create(this.indices);

    [Benchmark]
    public SeedKey GetCachedAuthorityKey() => this.authority.GetSeedKey(this.credit);

    [Benchmark]
    public string CleanupUnchangedInput() => "ordinary clean input".CleanupInput();

    [Benchmark]
    public string CleanupControlCharacters() => "  a\tb\rc\nd  ".CleanupInput();

    [Benchmark]
    public bool CompareProofSignatures() => this.proof.Equals(this.proofCopy);

    [Benchmark]
    public string FormatMergers() => this.mergers.ToMergerString(default);

    [Benchmark]
    public string[] ListVaultNames() => this.vault.GetNames();

    [Benchmark]
    public string[] ListVaultNamesByPrefix() => this.vault.GetNames("Authority/");
}
