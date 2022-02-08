// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using SimpleCommandLine;
using Tinyhand;

namespace ZenItzTest;

[TinyhandObject]
public partial struct IntPayload : IItzPayload
{
    public IntPayload()
    {
        this.Data = 0;
    }

    public IntPayload(int data)
    {
        this.Data = data;
    }

    [Key(0)]
    public int Data;
}

public record struct IntPayloadTest : IItzPayload
{
    [Key(0)]
    public int Data;
}

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record struct IntPayload2(int Data2) : IItzPayload;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record class IntPayload3(int Data2) : IItzPayload;

[SimpleCommand("basic")]
public class BasicTestSubcommand : ISimpleCommandAsync<BasicTestOptions>
{
    public BasicTestSubcommand(ZenControl zenControl)
    {
        this.ZenControl = zenControl;
    }

    public async Task Run(BasicTestOptions options, string[] args)
    {
        var zen = this.ZenControl.Zen;
        var itz = this.ZenControl.Itz;

        itz.Register(new ItzShip<IntPayload>(1_000_000));
        itz.Register(new ItzShip<IntPayload2>(3));

        var sw = Stopwatch.StartNew();
        Console.WriteLine($"Loaded: {await itz.LoadAsync("itz.test")}, {itz.TotalCount()}");

        var x = new IntPayload(1);
        itz.Set(Identifier.One, Identifier.One, ref x);
        var result = itz.Get<IntPayload>(Identifier.One, Identifier.One, out var x2);

        for (var i = 2; i <= 1_000_000; i++)
        {
            x = new(i);
            itz.Set(new Identifier(i), new Identifier(i), ref x);
        }

        Console.WriteLine("Set");

        Console.WriteLine($"Saved: {await itz.SaveAsync("itz.test")}, {itz.TotalCount()}");
        Console.WriteLine($"{sw.ElapsedMilliseconds} ms");
    }

    public ZenControl ZenControl { get; set; }
}

public record BasicTestOptions
{
    // [SimpleOption("node", description: "Node address", Required = true)]
    // public string Node { get; init; } = string.Empty;

    public override string ToString() => $"";
}
