// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using SimpleCommandLine;
using Tinyhand;

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
        var p = zen.CreateOrGet(Identifier.Zero);
        p.Set(new byte[] { 0, 1, });
        p.Save(true);
        var result = await p.Get();

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
