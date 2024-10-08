// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Arc.Threading;
using Lp.Data;
using Lp.T3cs;
using Netsphere.Crypto;
using Netsphere.Misc;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("test")]
public class TestSubcommand : ISimpleCommandAsync<TestOptions>
{
    public TestSubcommand(ILogger<TestSubcommand> logger, IUserInterfaceService userInterfaceService, Control control, AuthorityVault authorityVault, Seedphrase seedPhrase, LpStats lpStats)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.control = control;
        this.authorityVault = authorityVault;
        this.seedPhrase = seedPhrase;
    }

    public async Task RunAsync(TestOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"Test subcommand: {options.ToString()}");

        var microSleep = new Arc.Threading.MicroSleep();
        this.logger.TryGet()?.Log($"MicroSleep: {microSleep.CurrentMode.ToString()}");

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        microSleep.Sleep(1_000);
        var microSeconds = (double)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1_000_000;
        this.logger.TryGet()?.Log($"{microSeconds}");

        microSleep.Dispose();

        var mics = Mics.GetCorrected();
        this.userInterfaceService.WriteLine($"Mics: {mics}");
        this.userInterfaceService.WriteLine($"Utc: {Mics.ToDateTime(mics).ToString()}");
        this.userInterfaceService.WriteLine($"Hour: {Mics.ToDateTime(mics / Mics.MicsPerHour * Mics.MicsPerHour).ToString()}");
        this.userInterfaceService.WriteLine($"Day: {Mics.ToDateTime(mics / Mics.MicsPerDay * Mics.MicsPerDay).ToString()}");

        await this.TestLinkageKey();
    }

    private async Task TestLinkageKey()
    {
        var bt = new BenchTimer();
        var privateKey = SignaturePrivateKey.Create();
        var publicKey = privateKey.ToPublicKey();
        this.userInterfaceService.WriteLine($"Private(verification): {privateKey.UnsafeToString()}");
        this.userInterfaceService.WriteLine($"Public(verification): {publicKey.ToString()}");

        bt.Start();
        var privateEncryptionKey = EncryptionPrivateKey.Create();
        this.userInterfaceService.WriteLine($"Create encryption key: {bt.StopAndGetText()}");

        var publicEncryptionKey = privateEncryptionKey.ToPublicKey();
        this.userInterfaceService.WriteLine($"Private(encryption): {privateEncryptionKey.UnsafeToString()}");
        this.userInterfaceService.WriteLine($"Public(encryption): {publicEncryptionKey.ToString()}");

        // CryptoKey (Raw)
        var cryptoKey = CryptoKey.CreateRaw(publicKey);
        this.userInterfaceService.WriteLine($"CryptoKey (Raw) : {cryptoKey.ToString()}");
        this.userInterfaceService.WriteLine($"IsOriginalKey: {cryptoKey.IsOriginalKey(privateKey)}");
        if (cryptoKey.TryGetRawKey(out var originalKey))
        {
            this.userInterfaceService.WriteLine($"CryptoKey.TryGetRawKey() success.");
            this.userInterfaceService.WriteLine($"{originalKey.Equals(publicKey)}");
        }

        if (CryptoKey.TryParse(cryptoKey.ToString(), out var cryptoKey2))
        {
            var eq = cryptoKey.Equals(cryptoKey2);
        }

        // CryptoKey (Encrypted)
        var mergerKey = EncryptionPrivateKey.Create();
        uint encryption = 0;
        if (CryptoKey.TryCreateEncrypted(privateKey, mergerKey.ToPublicKey(), encryption, out cryptoKey))
        {
            this.userInterfaceService.WriteLine($"CryptoKey (Encrypted): {cryptoKey.ToString()}");

            this.userInterfaceService.WriteLine($"IsOriginalKey: {cryptoKey.IsOriginalKey(privateKey)}");

            if (cryptoKey.TryGetEncryptedKey(mergerKey, out originalKey) &&
                publicKey.Equals(originalKey))
            {
                this.userInterfaceService.WriteLine($"CryptoKey.TryGetEncryptedKey() success.");
            }
        }

        // if (await this.authorityVault.GetAuthority("lp") is { } authority)
        {
            var g = new CredentialProof.GoshujinClass();

            var owner = SignaturePrivateKey.Create();
            if (Credit.TryCreate(LpConstants.LpPublicKey, [SignaturePrivateKey.Create().ToPublicKey()], out var credit) &&
                Value.TryCreate(owner.ToPublicKey(), 111, credit, out var value))
            {
                this.userInterfaceService.WriteLine($"Credit: {credit.ToString()}");
                this.userInterfaceService.WriteLine($"Value: {value.ToString()}");
                Value.TryParse(value.ToString(), out var value2);
                this.userInterfaceService.WriteLine($"{value.Equals(value2)}");

                var valueProof = ValueProof.Create(value);

                valueProof.SignProof(owner, 123);
                var c = new CredentialProof();
            }
        }
    }

    private async Task Test0()
    {
        ECParameters key = default;
        key.Curve = ECCurve.CreateFromFriendlyName(KeyHelper.CurveInstance.CurveName);

        var st = this.seedPhrase.Create();
        var seed = this.seedPhrase.TryGetSeed(st);
        if (seed != null)
        {
            var pk = SignaturePrivateKey.Create(seed);
        }

        var privateKey = NodePrivateKey.AlternativePrivateKey;
        var publicKey = privateKey.ToPublicKey();

        this.userInterfaceService.WriteLine($"Alternative(private): {privateKey.UnsafeToString()}");
        this.userInterfaceService.WriteLine($"Length: {TinyhandSerializer.Serialize(privateKey).Length.ToString()}");
        this.userInterfaceService.WriteLine(TinyhandSerializer.SerializeToString(privateKey));
        this.userInterfaceService.WriteLine();

        this.userInterfaceService.WriteLine($"Alternative(public): {publicKey.ToString()}");
        this.userInterfaceService.WriteLine($"Length: {TinyhandSerializer.Serialize(publicKey).Length.ToString()}");
        this.userInterfaceService.WriteLine(TinyhandSerializer.SerializeToString(publicKey));
    }

    private readonly ILogger logger;
    private readonly Control control;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly AuthorityVault authorityVault;
    private readonly Seedphrase seedPhrase;
}

public record TestOptions
{
    [SimpleOption("Node", Description = "Node address")]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("Count", Description = "Count")]
    public int Count { get; init; }

    public override string ToString() => $"{this.Node}";
}
