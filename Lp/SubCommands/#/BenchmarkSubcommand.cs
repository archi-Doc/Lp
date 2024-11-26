// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Misc;
using SimpleCommandLine;

namespace Lp.Subcommands;

[SimpleCommand("benchmark", Description = "Executes a simple benchmark")]
public class BenchmarkSubcommand : ISimpleCommandAsync<BenchmarkOptions>
{
    public const int MaxRepetitions = 100;
    public const string CurveName = "secp256r1";
    public const string TestKeyString = "0, b\"9KfxBVYHXco5UZop78r+nv1BBuvb8TDozUgNPstvn7E=\", b\"I5dyWNPVlERjkHJ18u7AhVO2ElL2vExVYY8lILGnhWU=\", b\"HcvEcMJz+1SG59GNp3RWYAM4ejoEQ3bLWHA+rVIyfVQ=\"";

    public BenchmarkSubcommand(ILogger<BenchmarkSubcommand> logger, IUserInterfaceService userInterfaceService)
    {
        this.logger = logger;
        this.userInterfaceService = userInterfaceService;

        try
        {
            // var testKeyString = TinyhandSerializer.SerializeToString(AuthorityPrivateKey.Create());
            this.seedKey = SeedKey.NewSignature();
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

    private async Task RunBenchmark(BenchmarkOptions options)
    {
        await this.RunCryptoBenchmark(options);
        await this.RunCrypto2Benchmark(options);
        await this.RunSerializeBenchmark(options);
    }

    private async Task RunCryptoBenchmark(BenchmarkOptions options)
    {
        if (this.seedKey == null)
        {
            this.logger.TryGet(LogLevel.Error)?.Log("No ECDsa key.");
            return;
        }

        this.userInterfaceService.WriteLine($"Key: {this.seedKey.ToString()}");

        var bytes = TinyhandSerializer.Serialize(TestKeyString);

        var benchTimer = new BenchTimer();
        for (var r = 0; r < options.Repetition; r++)
        {
            ThreadCore.Root.CancellationToken.ThrowIfCancellationRequested();

            benchTimer.Start();

            var sign = new byte[CryptoSign.SignatureSize];
            for (var i = 0; i < 1000; i++)
            {
                this.seedKey.Sign(bytes, sign);
                var valid = this.seedKey.GetSignaturePublicKey().Verify(bytes, sign);
            }

            this.userInterfaceService.WriteLine(benchTimer.StopAndGetText());
        }

        this.logger.TryGet()?.Log(benchTimer.GetResult("Sign & Verify"));
    }

    private async Task RunCrypto2Benchmark(BenchmarkOptions options)
    {
        if (this.seedKey == null)
        {
            this.logger.TryGet(LogLevel.Error)?.Log("No seed key.");
            return;
        }

        this.userInterfaceService.WriteLine($"Public key compression success.");
        var bytes = TinyhandSerializer.Serialize(TestKeyString);

        var benchTimer = new BenchTimer();
        for (var r = 0; r < options.Repetition; r++)
        {
            ThreadCore.Root.CancellationToken.ThrowIfCancellationRequested();

            benchTimer.Start();

            var sign = new byte[CryptoSign.SignatureSize];
            for (var i = 0; i < 1000; i++)
            {
                this.seedKey.Sign(bytes, sign);
                var valid = this.seedKey.GetSignaturePublicKey().Verify(bytes, sign);
            }

            this.userInterfaceService.WriteLine(benchTimer.StopAndGetText());
        }

        this.logger.TryGet()?.Log(benchTimer.GetResult("Sign & Decompress & Verify"));
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

            this.userInterfaceService.WriteLine(benchTimer.StopAndGetText());
        }

        this.logger.TryGet()?.Log(benchTimer.GetResult("Serialize & Deserialize"));
    }

    private IUserInterfaceService userInterfaceService;
    private ILogger<BenchmarkSubcommand> logger;
    private SeedKey? seedKey;
}

public record BenchmarkOptions
{
    [SimpleOption("Repetition", ShortName = "r", Description = "Number of repetitions")]
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
