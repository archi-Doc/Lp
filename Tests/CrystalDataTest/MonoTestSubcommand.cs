// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using SimpleCommandLine;
using Tinyhand;

namespace CrystalDataTest;

[TinyhandObject]
public partial struct IntData : IMonoData
{
    public IntData()
    {
        this.Data = 0;
        this.Data2 = 0;
        this.Data3 = default!;
    }

    public IntData(int data)
    {
        this.Data = data;
        this.Data2 = 0;
        this.Data3 = default!;
    }

    [Key(0)]
    public int Data;

    [Key(1)]
    public long Data2;

    [Key(2)]
    public string Data3;
}

public record struct IntDataTest : IMonoData
{
    [Key(0)]
    public int Data;
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record struct IntData2(int Data2) : IMonoData;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record class IntData3(int Data2) : IMonoData;

[SimpleCommand("monotest")]
public class MonoTestSubcommand : ISimpleCommandAsync<BasicTestOptions>
{
    public MonoTestSubcommand(CrystalControl crystalControl)
    {
        this.CrystalControl = crystalControl;
    }

    public async Task RunAsync(BasicTestOptions options, string[] args)
    {
        var mono = new Mono();

        mono.Register(new Mono.StandardGroup<IntData>(1_000_000));
        mono.Register(new Mono.StandardGroup<IntData2>(3));

        var sw = Stopwatch.StartNew();
        Console.WriteLine($"Loaded: {await mono.LoadAsync("mono.test")}, {mono.TotalCount()}");

        var x = new IntData(1);
        mono.Set(in Identifier.One, in x);
        var result = mono.TryGet<IntData>(in Identifier.One, out var x2);

        for (var i = 2; i <= 1_000_000; i++)
        {
            x = new(i);
            var p = new Identifier(i);
            var p2 = new Identifier(i);
            mono.Set(in p, in x);
        }

        Console.WriteLine("Set");

        Console.WriteLine($"Saved: {await mono.SaveAsync("mono.test")}, {mono.TotalCount()}");
        Console.WriteLine($"{sw.ElapsedMilliseconds} ms"); // 2300ms -> 1800ms -> 
    }

    public CrystalControl CrystalControl { get; set; }
}

public record BasicTestOptions
{
    // [SimpleOption("node", Description = "Node address", Required = true)]
    // public string Node { get; init; } = string.Empty;

    public override string ToString() => $"";
}
