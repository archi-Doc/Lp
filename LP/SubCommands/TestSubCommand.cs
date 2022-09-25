// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Crypto;
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

        var privateKey = NodeKey.AlternativePrivateKey;
        var publicKey = new NodePublicKey(privateKey);

        Console.WriteLine($"Alternative(private): {privateKey.ToString()}");
        Console.WriteLine($"Length: {TinyhandSerializer.Serialize(privateKey).Length.ToString()}");
        Console.WriteLine(TinyhandSerializer.SerializeToString(privateKey));
        Console.WriteLine();

        Console.WriteLine($"Alternative(public): {publicKey.ToString()}");
        Console.WriteLine($"Length: {TinyhandSerializer.Serialize(publicKey).Length.ToString()}");
        Console.WriteLine(TinyhandSerializer.SerializeToString(publicKey));

        var originator = PrivateKey.Create();
        var pub = new PublicKey(originator);
        var value = new Value(1, pub, new[] { pub, });
        Console.WriteLine(value.GetHashCode());

        var bin = TinyhandSerializer.Serialize(value);
        var sign = originator.SignData(bin);
        var flag = pub.VerifyData(bin, sign);
    }

    public Control Control { get; set; }

    private ILogger<TestSubcommand> logger;
}

public record TestOptions
{
    [SimpleOption("node", description: "Node address")]
    public string Node { get; init; } = string.Empty;

    [SimpleOption("count", description: "Count")]
    public int Count { get; init; }

    public override string ToString() => $"{this.Node}";
}
