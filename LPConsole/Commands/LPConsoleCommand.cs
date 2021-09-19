// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
        public LPConsoleCommand(LPInfo info, LPCore core)
        {
            this.Info = info;
            this.Info.IsConsole = true;

            this.LPCore = core;
            this.LPCore.Initialize(true, string.Empty);
        }

        public async Task Run(LPConsoleOptions option, string[] args)
        {
            this.LPCore.Prepare(option);
            this.LPCore.Start();

            ThreadCore.Root.Sleep(1000);

            this.LPCore.Terminate();
        }

        public LPInfo Info { get; }

        public LPCore LPCore { get; }
    }
}
