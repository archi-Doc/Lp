// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Security.Cryptography;
using Lp.T3cs;
using Netsphere.Crypto;
using Netsphere.Misc;
using SimpleCommandLine;

namespace LP.Subcommands;

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

        var rawLinkageKey = LinkageKey.CreateRaw(publicKey);
        this.userInterfaceService.WriteLine($"Raw: {rawLinkageKey.ToString()}");
        bt.Start();
        LinkageKey.TryCreateEncrypted(publicKey, publicEncryptionKey, out var encryptedLinkageKey);
        this.userInterfaceService.WriteLine($"Create encrypted linkage key: {bt.StopAndGetText()}");
        this.userInterfaceService.WriteLine($"Encrypted: {encryptedLinkageKey.ToString()}");

        bt.Start();
        encryptedLinkageKey.TryDecrypt(privateEncryptionKey.TryGetEcdh()!, out var decryptedLinkageKey);
        this.userInterfaceService.WriteLine($"Decrypt linkage key: {bt.StopAndGetText()}");
        this.userInterfaceService.WriteLine($"Decrypted: {decryptedLinkageKey.ToString()}");

        var ecdh = privateEncryptionKey.TryGetEcdh()!;
        bt.Start();
        encryptedLinkageKey.TryDecrypt(ecdh, out decryptedLinkageKey);
        this.userInterfaceService.WriteLine($"Decrypt linkage key2: {bt.StopAndGetText()}");
        bt.Start();
        encryptedLinkageKey.TryDecrypt(ecdh, out decryptedLinkageKey);
        this.userInterfaceService.WriteLine($"Decrypt linkage key2: {bt.StopAndGetText()}");

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
    [SimpleOption("node", Description = "Node address")]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("count", Description = "Count")]
    public int Count { get; init; }

    public override string ToString() => $"{this.Node}";
}
