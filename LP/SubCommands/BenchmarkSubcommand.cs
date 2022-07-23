// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography;
using Arc.Crypto;
using LP;
using SimpleCommandLine;
using Tinyhand;

namespace LP.Subcommands;

[SimpleCommand("benchmark", "Executes a simple benchmark")]
public class BenchmarkSubcommand : ISimpleCommandAsync<BenchmarkOptions>
{
    public const int MaxRepetitions = 100;
    public const string CurveName = "secp256r1";
    public const string TestKeyString = "{\"test\", b\"AMlTJh7A1Bn7ltdGW5MCM5IcdyyIFNcgHl3HMEGFGhs=\", b\"cMXxoQ5zknPMhpR+8XVkxPwBaGQX4NY7U25OhRg/gRs=\", b\"d2oC+4V2Rufl6xKhFBqD5gNlSARat3Nejt08LhEYt9c=\"}";

    public BenchmarkSubcommand(ILogger<BenchmarkSubcommand> logger, Control control)
    {
        this.logger = logger;
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

    public async Task RunAsync(BenchmarkOptions options, string[] args)
    {
        if (options.Repetition < 1)
        {
            options.Repetition = 1;
        }
        else if (options.Repetition > MaxRepetitions)
        {
            options.Repetition = MaxRepetitions;
        }

        this.logger.TryGet()?.Log($"Benchmark subcommand: {options.ToString()}");

        await this.RunBenchmark(options);
    }

    public Control Control { get; set; }

    private async Task RunBenchmark(BenchmarkOptions options)
    {
        await this.RunCryptoBenchmark(options);
        await this.RunSerializeBenchmark(options);
    }

    private async Task RunCryptoBenchmark(BenchmarkOptions options)
    {
        if (this.testKey == null)
        {
            this.logger.TryGet(LogLevel.Error)?.Log("No ECDsa key.");
            return;
        }

        var bytes = TinyhandSerializer.Serialize(TestKeyString);

        var benchTimer = new BenchTimer();
        for (var r = 0; r < options.Repetition; r++)
        {
            ThreadCore.Root.CancellationToken.ThrowIfCancellationRequested();

            benchTimer.Start();

            for (var i = 0; i < 1000; i++)
            {
                var sign = this.testKey.SignData(bytes, HashAlgorithmName.SHA256);
                var valid = this.testKey.VerifyData(bytes, sign, HashAlgorithmName.SHA256);
            }

            Console.WriteLine(benchTimer.StopAndGetText());
        }

        this.logger.TryGet()?.Log(benchTimer.GetResult("Sign & Verify"));
    }

    private async Task RunSerializeBenchmark(BenchmarkOptions options)
    {
        var obj = new ObjectH2H();

        var benchTimer = new BenchTimer();
        for (var r = 0; r < options.Repetition; r++)
        {
            ThreadCore.Root.CancellationToken.ThrowIfCancellationRequested();

            benchTimer.Start();

            for (var i = 0; i < 1_000_000; i++)
            {
                TinyhandSerializer.Deserialize<ObjectH2H>(TinyhandSerializer.Serialize(obj));
            }

            Console.WriteLine(benchTimer.StopAndGetText());
        }

        this.logger.TryGet()?.Log(benchTimer.GetResult("Serialize & Deserialize"));
    }

    private ILogger<BenchmarkSubcommand> logger;
    private ECDsa? testKey;
}

public record BenchmarkOptions
{
    [SimpleOption("repetition", "r", description: "Number of repetitions")]
    public int Repetition { get; set; } = 3;
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
