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
    public TestSubcommand(ILogger<TestSubcommand> logger, Control control)
    {
        this.logger = logger;
        this.Control = control;
    }

    public async Task RunAsync(TestOptions options, string[] args)
    {
        this.logger.TryGet()?.Log($"Test subcommand: {options.ToString()}");

        ECParameters key = default;
        key.Curve = ECCurve.CreateFromFriendlyName(PublicKey.ECCurveName);

        byte[]? d = null;
        var hash = Hash.ObjectPool.Get();
        d = hash.GetHash(new byte[] { 0, });

        key.D = d;
        var ecdsa = ECDsa.Create(key);
        key = ecdsa.ExportParameters(true);

        Array.Fill<byte>(d, 255);
        key.D = d;
        ecdsa = ECDsa.Create(key);
        key = ecdsa.ExportParameters(true); // 00000000ffffffff00000000000000004319055258e8617b0c46353d039cdaae

        d = Arc.Crypto.Hex.FromStringToByteArray("00000000ffffffff00000000000000004319055258e8617b0c46353d039cdaae");
        key.D = d;
        ecdsa = ECDsa.Create(key);
        key = ecdsa.ExportParameters(true);

        var privateKey = NodePrivateKey.AlternativePrivateKey;
        var publicKey = privateKey.ToPublicKey();

        Console.WriteLine($"Alternative(private): {privateKey.ToString()}");
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
    }

    public Control Control { get; set; }

    private ILogger<TestSubcommand> logger;
}

public record TestOptions
{
    [SimpleOption("node", Description = "Node address")]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("count", Description = "Count")]
    public int Count { get; init; }

    public override string ToString() => $"{this.Node}";
}
