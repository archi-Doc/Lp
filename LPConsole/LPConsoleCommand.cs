// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Arc.Crypto;
using Arc.Threading;
using CrossChannel;
using LP;
using LP.Options;
using LP.Unit;
using Microsoft.Extensions.DependencyInjection;
using Netsphere;
using SimpleCommandLine;
using Tinyhand;
using ZenItz;

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
