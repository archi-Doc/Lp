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
    public TestSubcommand(ILogger<TestSubcommand> logger, IUserInterfaceService userInterfaceService, Control control, AuthorityControl authorityControl, Seedphrase seedPhrase, LpStats lpStats)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.control = control;
        this.authorityControl = authorityControl;
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

        await this.TestLinkageKey();
    }

    private async Task TestLinkageKey()
    {
        var seedKey = SeedKey.New(KeyOrientation.Signature);
        var signaturePublicKey = seedKey.GetSignaturePublicKey();
        var encryptionPublicKey = seedKey.GetEncryptionPublicKey();
        this.userInterfaceService.WriteLine($"SeedKey: {seedKey.UnsafeToString()}");
        this.userInterfaceService.WriteLine($"Public(signature): {signaturePublicKey.ToString()}");
        this.userInterfaceService.WriteLine($"Public(encryption): {encryptionPublicKey.ToString()}");

        // CryptoKey (Raw)
        /*var cryptoKey = CryptoKey.CreateRaw(publicKey);
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
        }*/

        // if (await this.authorityControl.GetAuthority("lp") is { } authority)
        {
            var g = new CredentialProof.GoshujinClass();

            var owner = SeedKey.NewSignature();
            if (Credit.TryCreate(LpConstants.LpPublicKey, [SeedKey.NewSignature().GetSignaturePublicKey()], out var credit) &&
                Value.TryCreate(owner.GetSignaturePublicKey(), 111, credit, out var value))
            {
                this.userInterfaceService.WriteLine($"Credit: {credit.ToString()}");
                this.userInterfaceService.WriteLine($"Value: {value.ToString()}");
                Value.TryParse(value.ToString(), out var value2, out _);
                this.userInterfaceService.WriteLine($"{value.Equals(value2)}");

                var valueProof = ValueProof.Create(value);

                owner.TrySign(valueProof, 123);
            }
        }
    }

    private readonly ILogger logger;
    private readonly Control control;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly AuthorityControl authorityControl;
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
