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
        for (var i = 0; i < repetitions; i++)
        {
            await this.RunBenchmark();
        }
    }

    public Control Control { get; set; }

    private async Task RunBenchmark()
    {
        await this.RunCryptoBenchmark();
    }

    private async Task RunCryptoBenchmark()
    {
        if (this.testKey == null)
        {
            Logger.Subcommand.Error("No ECDsa key.");
            return;
        }

        var bytes = TinyhandSerializer.Serialize(TestKeyString);
        var sw = Stopwatch.StartNew();

        for (var i = 0; i < 1000; i++)
        {
            var sign = this.testKey.SignData(bytes, HashAlgorithmName.SHA256);
            var valid = this.testKey.VerifyData(bytes, sign, HashAlgorithmName.SHA256);
        }

        sw.Stop();

        Console.WriteLine($"Sign & verify: {sw.ElapsedMilliseconds.ToString()} ms");
    }

    private ECDsa? testKey;
}

public record BenchmarkOptions
{
    [SimpleOption("repetition", "r", description: "Number of repetitions")]
    public int Repetition { get; init; } = 2;
}
