// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Lp.T3cs;
using Netsphere.Crypto;
using Netsphere.Misc;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("test")]
public class TestSubcommand : ISimpleCommandAsync<TestOptions>
{
    public TestSubcommand(ILogger<TestSubcommand> logger, IUserInterfaceService userInterfaceService, Control control, AuthorityVault authorityVault, Seedphrase seedPhrase)
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

        /*var privateKey = NodePrivateKey.Create();
        var publicKey = privateKey.ToPublicKey();
        Console.WriteLine($"{privateKey.UnsafeToString()}");
        Console.WriteLine($"{publicKey.ToString()}");

        var st = privateKey.UnsafeToString();
        NodePrivateKey.TryParse(st, out var privateKey2);
        Console.WriteLine($"{privateKey.Equals(privateKey2).ToString()}");
        st = publicKey.ToString();
        NodePublicKey.TryParse(st, out var publicKey2);
        Console.WriteLine($"{publicKey.Equals(publicKey2).ToString()}");*/

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

        if (await this.authorityVault.GetAuthority("lp") is { } authority)
        {
            var g = new Credential.GoshujinClass();
            var c = new Credential(new());
            authority.Sign(c);
            var integrality = Credential.Integrality.Pool.Get();
            integrality.IntegrateObject(g, c);
            Credential.Integrality.Pool.Return(integrality);
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

        var originator = SignaturePrivateKey.Create();
        var pub = originator.ToPublicKey();
        var value = new Value(1, pub, [pub]);
        this.userInterfaceService.WriteLine(value.GetHashCode().ToString());

        var bin = TinyhandSerializer.Serialize(value);
        var sign = originator.SignData(bin);
        var flag = pub.VerifyData(bin, sign);

        this.userInterfaceService.WriteLine($"Originator: {originator.ToString()}, {flag.ToString()}");
        this.userInterfaceService.WriteLine($"{pub.ToString()}");

        // this.userInterfaceService.WriteLine(HashedString.FromEnum(CrystalResult.NoStorage));

        /*using (var terminal = this.control.NetControl.TerminalObsolete.TryCreate(NetNode.Alternative))
        {
            if (terminal is null)
            {
                return;
            }

            var service = terminal.GetService<IBenchmarkService>();
            await service.Report(new());
        }*/
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
