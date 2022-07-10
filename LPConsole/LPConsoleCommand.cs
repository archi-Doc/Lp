// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.Options;
using SimpleCommandLine;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace LPConsole;

[SimpleCommand("lp", Default = true)]
public class LPConsoleCommand : ISimpleCommandAsync<LPOptions>
{
    public LPConsoleCommand(Control.Unit unit)
    {
        this.unit = unit;
    }

    public async Task Run(LPOptions options, string[] args)
    {
        await this.unit.Run(options);
    }

    private Control.Unit unit;
}
