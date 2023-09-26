// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using LP.NetServices;
using LP.T3CS;
using SimpleCommandLine;

namespace LP.Subcommands;

[SimpleCommand("test")]
public class TestSubcommand : ISimpleCommandAsync<TestOptions>
{
    public TestSubcommand(ILogger<TestSubcommand> logger, IUserInterfaceService userInterfaceService, Control control, Seedphrase seedPhrase)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;
        this.control = control;
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
        var privateKey = SignaturePrivateKey.CreateSignatureKey();
        var publicKey = privateKey.ToPublicKey();
        this.userInterfaceService.WriteLine($"Private(verification): {privateKey.UnsafeToString()}");
        this.userInterfaceService.WriteLine($"Public(verification): {publicKey.ToString()}");

        bt.Start();
        var privateEncryptionKey = SignaturePrivateKey.CreateEncryptionKey();
        this.userInterfaceService.WriteLine($"Create encryption key: {bt.StopAndGetText()}");

        var publicEncryptionKey = privateEncryptionKey.ToPublicKey();
        this.userInterfaceService.WriteLine($"Private(encryption): {privateEncryptionKey.UnsafeToString()}");
        this.userInterfaceService.WriteLine($"Public(encryption): {publicEncryptionKey.ToString()}");

        var rawLinkageKey = LinkageKey.CreateRaw(publicKey);
        this.userInterfaceService.WriteLine($"Raw: {rawLinkageKey.ToString()}");
        bt.Start();
        var encryptedLinkageKey = LinkageKey.CreateEncrypted(publicKey, publicEncryptionKey);
        this.userInterfaceService.WriteLine($"Create encrypted linkage key: {bt.StopAndGetText()}");
        this.userInterfaceService.WriteLine($"Encrypted: {encryptedLinkageKey.ToString()}");

        bt.Start();
        encryptedLinkageKey.TryDecrypt(privateEncryptionKey, out var decryptedLinkageKey);
        this.userInterfaceService.WriteLine($"Decrypt linkage key: {bt.StopAndGetText()}");
        this.userInterfaceService.WriteLine($"Decrypted: {decryptedLinkageKey.ToString()}");

        var ecdh = privateEncryptionKey.TryGetEcdh()!;
        bt.Start();
        encryptedLinkageKey.TryDecrypt(ecdh, out decryptedLinkageKey);
        this.userInterfaceService.WriteLine($"Decrypt linkage key2: {bt.StopAndGetText()}");
        bt.Start();
        encryptedLinkageKey.TryDecrypt(ecdh, out decryptedLinkageKey);
        this.userInterfaceService.WriteLine($"Decrypt linkage key2: {bt.StopAndGetText()}");

        var engageProof = new EngageProof(1);
        engageProof.SignProof(privateKey, 1);
        var result = engageProof.ValidateAndVerify();
    }

    private async Task Test0()
    {
        ECParameters key = default;
        key.Curve = ECCurve.CreateFromFriendlyName(KeyHelper.CurveInstance.CurveName);

        var st = this.seedPhrase.Create();
        var seed = this.seedPhrase.TryGetSeed(st);
        if (seed != null)
        {
            var pk = SignaturePrivateKey.CreateSignatureKey(seed);
        }

        var privateKey = NodePrivateKey.AlternativePrivateKey;
        var publicKey = privateKey.ToPublicKey();

        this.userInterfaceService.WriteLine($"Alternative(private): {privateKey.ToUnsafeString()}");
        this.userInterfaceService.WriteLine($"Length: {TinyhandSerializer.Serialize(privateKey).Length.ToString()}");
        this.userInterfaceService.WriteLine(TinyhandSerializer.SerializeToString(privateKey));
        this.userInterfaceService.WriteLine();

        this.userInterfaceService.WriteLine($"Alternative(public): {publicKey.ToString()}");
        this.userInterfaceService.WriteLine($"Length: {TinyhandSerializer.Serialize(publicKey).Length.ToString()}");
        this.userInterfaceService.WriteLine(TinyhandSerializer.SerializeToString(publicKey));

        var originator = SignaturePrivateKey.CreateSignatureKey();
        var pub = originator.ToPublicKey();
        var value = new Value(1, pub, new[] { pub, });
        this.userInterfaceService.WriteLine(value.GetHashCode().ToString());

        var bin = TinyhandSerializer.Serialize(value);
        var sign = originator.SignData(bin);
        var flag = pub.VerifyData(bin, sign);

        this.userInterfaceService.WriteLine($"Originator: {originator.ToString()}, {flag.ToString()}");
        this.userInterfaceService.WriteLine($"{pub.ToString()}");

        var token = new Token(Token.Type.Identification, 0, 0, Identifier.Three, null);
        var bb = token.Sign(originator);
        bb = token.ValidateAndVerifyWithoutPublicKey();

        originator.CreateSignature(value, out var signature);
        // this.userInterfaceService.WriteLine(HashedString.FromEnum(CrystalResult.NoStorage));

        using (var terminal = this.control.NetControl.Terminal.Create(Netsphere.NodeAddress.Alternative))
        {
            var service = terminal.GetService<IBenchmarkService>();
            await service.Report(new());
        }
    }

    private ILogger<TestSubcommand> logger;
    private Control control;
    private IUserInterfaceService userInterfaceService;
    private Seedphrase seedPhrase;
}

public record TestOptions
{
    [SimpleOption("node", Description = "Node address")]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("count", Description = "Count")]
    public int Count { get; init; }

    public override string ToString() => $"{this.Node}";
}
