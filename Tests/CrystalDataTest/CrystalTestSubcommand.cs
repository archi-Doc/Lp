// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using CrystalData.Datum;
using SimpleCommandLine;
using LP.Crystal;
using Tinyhand;

namespace CrystalDataTest;

[SimpleCommand("crystaltest")]
public class CrystalTestSubcommand : ISimpleCommandAsync<CrystalTestOptions>
{
    public CrystalTestSubcommand(CrystalControl crystalControl, IBigCrystal<LpData> crystal)
    {
        this.CrystalControl = crystalControl;
        this.crystal = crystal;
    }

    public async Task RunAsync(CrystalTestOptions options, string[] args)
    {
        var mono = new Mono();

        var sw = Stopwatch.StartNew();
        Console.WriteLine($"Start: {sw.ElapsedMilliseconds} ms");

        var data = this.crystal.Data.GetOrCreateChild(Identifier.Zero);
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

        data = this.crystal.Data.GetOrCreateChild(Identifier.One);
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

        var byteArray = new byte[BigCrystalConfiguration.DefaultMaxDataSize];
        for (var i = 0; i < 10; i++)
        {
            data = this.crystal.Data.GetOrCreateChild(new(i));
            if (data != null)
            {
                var dt = await data.BlockDatum().Get();
                data.BlockDatum().Set(byteArray);
            }
        }

        await Console.Out.WriteLineAsync("1M flakes");
        for (var i = 0; i < 1_000_000; i++)
        {
            data = this.crystal.Data.GetOrCreateChild(new(i));
            // flake.SetData(new byte[] { 2, 3, });
        }

        sw.Restart();
        var bin = TinyhandSerializer.SerializeObject(this.crystal.Data);
        Console.WriteLine($"Serialize {(bin.Length / 1_000_000).ToString()} MB, {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        await this.crystal.Save(UnloadMode.ForceUnload);
        Console.WriteLine($"Save {sw.ElapsedMilliseconds} ms");
    }

    public CrystalControl CrystalControl { get; set; }

    private IBigCrystal<LpData> crystal;
}

public record CrystalTestOptions
{
    // [SimpleOption("node", Description = "Node address", Required = true)]
    // public string Node { get; init; } = string.Empty;

    public override string ToString() => $"";
}
