// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using SimpleCommandLine;
using LP.Fragments;

namespace ZenItzTest;

[SimpleCommand("zentest")]
public class ZenTestSubcommand : ISimpleCommandAsync<ZenTestOptions>
{
    public ZenTestSubcommand(ZenControl zenControl)
    {
        this.ZenControl = zenControl;
    }

    public async Task RunAsync(ZenTestOptions options, string[] args)
    {
        var zen = this.ZenControl.Zen;
        var itz = this.ZenControl.Itz;

        var sw = Stopwatch.StartNew();
        await zen.StartAsync(new());
        Console.WriteLine($"Start: {sw.ElapsedMilliseconds} ms");

        var flake = zen.Root.GetOrCreateChild(Identifier.Zero);
        if (flake != null)
        {
            flake.BlockData.Set(new byte[] { 0, 1, });
            flake.Save(true);
            var result = await flake.BlockData.Get();
            flake.Remove();

            using (var block = flake.Lock<BlockData>())
            {// lock(flake.syncObject)
                if (block.Data is { } blockData)
                {
                    blockData.Set(new byte[] { 0, });
                }
            }
        }

        flake = zen.Root.GetOrCreateChild(Identifier.One);
        if (flake != null)
        {
            flake.BlockData.SetObject(new TestFragment());
            var t = await flake.BlockData.GetObject<TestFragment>();

            flake.Save(true);
            t = await flake.BlockData.GetObject<TestFragment>();

            flake.FragmentData.Set(Identifier.One, new byte[] { 2, 3, });
            var result = await flake.FragmentData.Get(Identifier.One);
            flake.Save(true);
            result = await flake.FragmentData.Get(Identifier.One);
            flake.FragmentData.Remove(Identifier.One);
            flake.Save(true);
            result = await flake.FragmentData.Get(Identifier.One);
            flake.Save(true);

            var tf = new TestFragment();
            tf.Name = "A";
            flake.FragmentData.SetObject(Identifier.One, tf);
            var tc = await flake.FragmentData.GetObject<TestFragment>(Identifier.One);
        }

        var data = new byte[ZenOptions.DefaultMaxDataSize];
        for (var i = 0; i < 10; i++)
        {
            flake = zen.Root.GetOrCreateChild(new(i));
            if (flake != null)
            {
                var dt = await flake.BlockData.Get();
                flake.BlockData.Set(data);
            }
        }

        await Console.Out.WriteLineAsync("1M flakes");
        sw.Restart();

        for (var i = 0; i < 1_000_000; i++)
        {
            flake = zen.Root.GetOrCreateChild(new(i));
            // flake.SetData(new byte[] { 2, 3, });
        }

        // await Task.Delay(10000);

        await zen.StopAsync(new());
        Console.WriteLine($"{sw.ElapsedMilliseconds} ms");
    }

    public ZenControl ZenControl { get; set; }
}

public record ZenTestOptions
{
    // [SimpleOption("node", Description = "Node address", Required = true)]
    // public string Node { get; init; } = string.Empty;

    public override string ToString() => $"";
}
