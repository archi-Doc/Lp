// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics;
using SimpleCommandLine;

namespace ZenItzTest;

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

        itz.Register(new ItzShip<int>(3));

        int x = 1;
        itz.Set<int>(Identifier.One, Identifier.One, ref x);
        var result = itz.Get<int>(Identifier.One, Identifier.One, out var y);

        for (x = 2; x < 5; x++)
        {
            itz.Set<int>(new Identifier(x), new Identifier(x), ref x);
        }

        var ship = itz.GetShip<int>();
        var ba = ship.Serialize();
        ship.Deserialize(ba);
    }

    public ZenControl ZenControl { get; set; }
}

public record BasicTestOptions
{
    // [SimpleOption("node", description: "Node address", Required = true)]
    // public string Node { get; init; } = string.Empty;

    public override string ToString() => $"";
}
