// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using CrystalData.Datum;
using SimpleCommandLine;
using LP.Crystal;

namespace CrystalDataTest;

[SimpleCommand("crystaltest")]
public class CrystalTestSubcommand : ISimpleCommandAsync<CrystalTestOptions>
{
    public CrystalTestSubcommand(CrystalControl crystalControl)
    {
        this.CrystalControl = crystalControl;
        this.crystal = this.CrystalControl.CreateCrystal<LpData>(CrystalOptions.Default);
    }

    public async Task RunAsync(CrystalTestOptions options, string[] args)
    {
        var mono = new Mono();

        var sw = Stopwatch.StartNew();
        await this.crystal.StartAsync(new());
        Console.WriteLine($"Start: {sw.ElapsedMilliseconds} ms");

        var data = this.crystal.Root.GetOrCreateChild(Identifier.Zero);
        if (data != null)
        {
            data.BlockDatum().Set(new byte[] { 0, 1, });
            data.Save(true);
            var result = await data.BlockDatum().Get();
            data.Delete();

            using (var block = data.Lock<BlockDatum>())
            {// lock(flake.syncObject)
                if (block.Datum is { } blockData)
                {
                    blockData.Set(new byte[] { 0, });
                }
            }
        }

        data = this.crystal.Root.GetOrCreateChild(Identifier.One);
        if (data != null)
        {
            data.BlockDatum().SetObject(new TestFragment());
            var t = await data.BlockDatum().GetObject<TestFragment>();

            data.Save(true);
            t = await data.BlockDatum().GetObject<TestFragment>();

            data.FragmentDatum().Set(Identifier.One, new byte[] { 2, 3, });
            var result = await data.FragmentDatum().Get(Identifier.One);
            data.Save(true);
            result = await data.FragmentDatum().Get(Identifier.One);
            data.FragmentDatum().Remove(Identifier.One);
            data.Save(true);
            result = await data.FragmentDatum().Get(Identifier.One);
            data.Save(true);

            var tf = new TestFragment();
            tf.Name = "A";
            data.FragmentDatum().SetObject(Identifier.One, tf);
            var tc = await data.FragmentDatum().GetObject<TestFragment>(Identifier.One);
        }

        var byteArray = new byte[CrystalOptions.DefaultMaxDataSize];
        for (var i = 0; i < 10; i++)
        {
            data = this.crystal.Root.GetOrCreateChild(new(i));
            if (data != null)
            {
                var dt = await data.BlockDatum().Get();
                data.BlockDatum().Set(byteArray);
            }
        }

        await Console.Out.WriteLineAsync("1M flakes");
        sw.Restart();

        for (var i = 0; i < 1_000_000; i++)
        {
            data = this.crystal.Root.GetOrCreateChild(new(i));
            // flake.SetData(new byte[] { 2, 3, });
        }

        // await Task.Delay(10000);

        await this.crystal.StopAsync(new());
        Console.WriteLine($"{sw.ElapsedMilliseconds} ms");
    }

    public CrystalControl CrystalControl { get; set; }

    private Crystal<LpData> crystal;
}

public record CrystalTestOptions
{
    // [SimpleOption("node", Description = "Node address", Required = true)]
    // public string Node { get; init; } = string.Empty;

    public override string ToString() => $"";
}
