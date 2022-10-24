// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Security.Cryptography;
using Arc.Crypto;
using Arc.Crypto.EC;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("test")]
public class TestSubcommand : ISimpleCommandAsync<TestOptions>
{
    public TestSubcommand(ILogger<TestSubcommand> logger, Control control, Seedphrase seedPhrase)
    {
        this.logger = logger;
        this.Control = control;
        this.seedPhrase = seedPhrase;
    }

    public async Task RunAsync(TestOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"Test subcommand: {options.ToString()}");

        ECParameters key = default;
        key.Curve = ECCurve.CreateFromFriendlyName(PublicKey.ECCurveName);

        var st = this.seedPhrase.Create();
        var seed = this.seedPhrase.TryGetSeed(st);
        if (seed != null)
        {
            var pk = PrivateKey.Create(seed);
        }

        var privateKey = NodePrivateKey.AlternativePrivateKey;
        var publicKey = privateKey.ToPublicKey();

        Console.WriteLine($"Alternative(private): {privateKey.ToUnsafeString()}");
        Console.WriteLine($"Length: {TinyhandSerializer.Serialize(privateKey).Length.ToString()}");
        Console.WriteLine(TinyhandSerializer.SerializeToString(privateKey));
        Console.WriteLine();

        Console.WriteLine($"Alternative(public): {publicKey.ToString()}");
        Console.WriteLine($"Length: {TinyhandSerializer.Serialize(publicKey).Length.ToString()}");
        Console.WriteLine(TinyhandSerializer.SerializeToString(publicKey));

        var originator = PrivateKey.Create("originator");
        var pub = originator.ToPublicKey();
        var value = new Value(1, pub, new[] { pub, });
        Console.WriteLine(value.GetHashCode());

        var bin = TinyhandSerializer.Serialize(value);
        var sign = originator.SignData(bin);
        var flag = pub.VerifyData(bin, sign);

        Console.WriteLine($"Originator: {originator.ToString()}, {flag.ToString()}");
        Console.WriteLine($"{pub.ToString()}");

        var token = new Token(Token.Type.Identification, 0, 0, Identifier.Three, null);
        var bb = token.Sign(originator);
        bb = token.ValidateAndVerify();
    }

    public Control Control { get; set; }

    private ILogger<TestSubcommand> logger;

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
