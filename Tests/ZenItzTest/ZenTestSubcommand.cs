// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using SimpleCommandLine;
using Tinyhand;
using LP.Fragments;

namespace ZenItzTest;

[SimpleCommand("zentest")]
public class ZenTestSubcommand : ISimpleCommandAsync<ZenTestOptions>
{
    public ZenTestSubcommand(ZenControl zenControl)
    {
        this.ZenControl = zenControl;
    }

    public async Task Run(ZenTestOptions options, string[] args)
    {
        var zen = this.ZenControl.Zen;
        var itz = this.ZenControl.Itz;

        await zen.TryStartZen(new(Zen.DefaultZenDirectory));

        var flake = zen.TryCreateOrGet(Identifier.Zero);
        if (flake != null)
        {
            flake.Set(new byte[] { 0, 1, });
            flake.Save(true);
            var result = await flake.Get();
            flake.Remove();
        }

        flake = zen.TryCreateOrGet(Identifier.One);
        if (flake != null)
        {
            flake.SetObject(new TestFragment());
            var t = await flake.GetObject<TestFragment>();

            flake.Save(true);
            t = await flake.GetObject<TestFragment>();

            flake.SetFragment(Identifier.One, new byte[] { 2, 3, });
            var result = await flake.GetFragment(Identifier.One);
            flake.Save(true);
            result = await flake.GetFragment(Identifier.One);
            flake.Remove(Identifier.One);
            flake.Save(true);
            result = await flake.GetFragment(Identifier.One);
            flake.Save(true);

            var tc = await flake.GetFragment<TestFragment>(Identifier.One);
        }

        await zen.StopZen(new());
    }

    public ZenControl ZenControl { get; set; }
}

public record ZenTestOptions
{
    // [SimpleOption("node", description: "Node address", Required = true)]
    // public string Node { get; init; } = string.Empty;

    public override string ToString() => $"";
}
