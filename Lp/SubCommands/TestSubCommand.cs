// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("test")]
public class TestSubcommand : ISimpleCommandAsync<TestOptions>
{
    public TestSubcommand(ILogger<TestSubcommand> logger, IUserInterfaceService userInterfaceService, LpUnit lpUnit, AuthorityControl authorityControl, LpBoardService lpBoardService)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.lpUnit = lpUnit;
        this.authorityControl = authorityControl;
        this.lpBoardService = lpBoardService;
    }

    public async Task RunAsync(TestOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"Test subcommand: {options.ToString()}");

        var path = await this.userInterfaceService.RequestString(true, "Input: ");
        this.userInterfaceService.WriteLine(path);

        try
        {
            var nn = Alternative.NetNode;
            var st = TinyhandSerializer.SerializeToString(nn);
            var mx = SeedKeyHelper.MaxPrivateKeyLengthInBase64;
            var creditIdentity = TinyhandSerializer.DeserializeFromString<CreditIdentity>(args[0].TrimQuotesAndBracket());
        }
        catch
        {
        }

        Console.WriteLine(LpConstants.LpPublicKey.ToString());
        Console.WriteLine(LpConstants.LpCredit.ToString());
        Console.WriteLine(TinyhandSerializer.SerializeToString(LpConstants.LpIdentity, TinyhandSerializerOptions.ConvertToSimpoleString));

        await this.lpBoardService.CreateBoard(SeedKey.NewSignature().GetSignaturePublicKey(), SeedKey.NewSignature().GetSignaturePublicKey());
        Console.WriteLine($"Width: {Console.WindowWidth}");

        var microSleep = new Arc.Threading.MicroSleep();
        this.logger.TryGet()?.Log($"MicroSleep: {microSleep.CurrentMode.ToString()}");

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        microSleep.Sleep(1_000);
        var microSeconds = (double)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1_000_000;
        this.logger.TryGet()?.Log($"{microSeconds}");

        microSleep.Dispose();

        // await this.TestLinkageKey();

        _ = Task.Run(() =>
        {
            Thread.Sleep(1_000);
            this.userInterfaceService.WriteLine("ABC");
        });
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
            var owner = SeedKey.NewSignature();
            var creditIdentity = new CreditIdentity(default, owner.GetSignaturePublicKey(), [SeedKey.NewSignature().GetSignaturePublicKey()]);
            if (Credit.TryCreate(creditIdentity, out var credit) &&
                Value.TryCreate(owner.GetSignaturePublicKey(), 111, credit, out var value))
            {
                this.userInterfaceService.WriteLine($"Credit: {credit.ToString()}");
                this.userInterfaceService.WriteLine($"Value: {value.ToString()}");
                Value.TryParse(value.ToString(), out var value2, out _);
                this.userInterfaceService.WriteLine($"{value.Equals(value2)}");

                var valueProof = new ValueProof(value);
                owner.TrySign(valueProof, 123);
            }
        }
    }

    private readonly ILogger logger;
    private readonly LpUnit lpUnit;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly AuthorityControl authorityControl;
    private readonly LpBoardService lpBoardService;
}

public record TestOptions
{
    [SimpleOption("Node", Description = "Node address")]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("Count", Description = "Count")]
    public int Count { get; init; }

    public override string ToString() => $"{this.Node}";
}
