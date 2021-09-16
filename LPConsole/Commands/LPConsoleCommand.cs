﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using LP;
using Serilog;
using SimpleCommandLine;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace LPConsole
{
    [SimpleCommand("lp", Default = true)]
    public class LPConsoleCommand : ISimpleCommandAsync<LPConsoleOptions>
    {
        public LPConsoleCommand(LPCore core)
        {
            this.LPCore = core;
            this.LPCore.Initialize(true, string.Empty);
        }

        public async Task Run(LPConsoleOptions option, string[] args)
        {
            this.LPCore.Prepare(option);
            this.LPCore.Start();

            await Task.Delay(1000);

            this.LPCore.Terminate();
        }

        public LPCore LPCore { get; }
    }
}
