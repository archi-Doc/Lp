// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("benchmark")]
public class BenchmarkSubcommand : ISimpleCommandAsync<BenchmarkOptions>
{
    public const int MaxRepetitions = 100;
    public const string CurveName = "secp256r1";
    public const string TestKeyString = "{\"test\", b\"AMlTJh7A1Bn7ltdGW5MCM5IcdyyIFNcgHl3HMEGFGhs=\", b\"cMXxoQ5zknPMhpR+8XVkxPwBaGQX4NY7U25OhRg/gRs=\", b\"d2oC+4V2Rufl6xKhFBqD5gNlSARat3Nejt08LhEYt9c=\"}";

    public BenchmarkSubcommand(Control control)
    {
        this.Control = control;

        try
        {
            // var testKeyString = TinyhandSerializer.SerializeToString(AuthorityPrivateKey.Create("test"));
            var privateKey = TinyhandSerializer.DeserializeFromString<AuthorityPrivateKey>(TestKeyString);
            if (privateKey != null)
            {
                this.testKey = Authority.FromPrivateKey(privateKey);
            }
        }
        catch
        {
        }
    }

    public async Task Run(BenchmarkOptions options, string[] args)
    {
        Logger.Subcommand.Information($"Benchmark subcommand: {options.ToString()}");

        var repetitions = options.Repetition < MaxRepetitions ? options.Repetition : MaxRepetitions;

        // for (var i = 0; i < repetitions; i++)
        await this.RunBenchmark(repetitions);
    }

    public Control Control { get; set; }

    private async Task RunBenchmark(int repetitions)
    {
        await this.RunCryptoBenchmark(repetitions);
        await this.RunSerializeBenchmark(repetitions);
    }

    private async Task RunCryptoBenchmark(int repetitions)
    {
        if (this.testKey == null)
        {
            Logger.Subcommand.Error("No ECDsa key.");
            return;
        }

        var bytes = TinyhandSerializer.Serialize(TestKeyString);

        var benchTimer = new BenchTimer();
        for (var r = 0; r < repetitions; r++)
        {
            benchTimer.Start();

            for (var i = 0; i < 1000; i++)
            {
                var sign = this.testKey.SignData(bytes, HashAlgorithmName.SHA256);
                var valid = this.testKey.VerifyData(bytes, sign, HashAlgorithmName.SHA256);
            }

            Console.WriteLine(benchTimer.StopAndGetText());
        }

        Console.WriteLine(benchTimer.GetResult("Sign & Verify"));
    }

    private async Task RunSerializeBenchmark(int repetitions)
    {
        var obj = new ObjectH2H();

        var benchTimer = new BenchTimer();
        for (var r = 0; r < repetitions; r++)
        {
            benchTimer.Start();

            for (var i = 0; i < 1_000_000; i++)
            {
                TinyhandSerializer.Deserialize<ObjectH2H>(TinyhandSerializer.Serialize(obj));
            }

            Console.WriteLine(benchTimer.StopAndGetText());
        }

        Console.WriteLine(benchTimer.GetResult("Serialize & Deserialize"));
    }

    private ECDsa? testKey;
}

public record BenchmarkOptions
{
    [SimpleOption("repetition", "r", description: "Number of repetitions")]
    public int Repetition { get; init; } = 3;
}

[TinyhandObject]
internal partial class ObjectH2H
{
    public const int ArrayN = 10;

    public ObjectH2H()
    {
        this.B = Enumerable.Range(0, ArrayN).ToArray();
    }

    [Key(0)]
    public int X { get; set; } = 0;

    [Key(1)]
    public int Y { get; set; } = 100;

    [Key(2)]
    public int Z { get; set; } = 10000;

    [Key(3)]
    public string A { get; set; } = "H2Htest";

    [Key(4)]
    public double C { get; set; } = 123456789;

    [Key(8)]
    public int[] B { get; set; } = new int[0];
}
