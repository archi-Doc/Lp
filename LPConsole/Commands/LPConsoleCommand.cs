// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using DryIoc;
using LP;
using Serilog;
using SimpleCommandLine;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace LPConsole
{
    [SimpleCommand("lp", Default = true)]
    public class LPConsoleCommand : ISimpleCommandAsync<LPConsoleOptions>
    {
        public LPConsoleCommand(Container container)
        {
            this.Container = container;
        }

        public async Task Run(LPConsoleOptions option, string[] args)
        {
            var info = this.Container.Resolve<LPInfo>();
            info.Configure(option, true);

            var core = this.Container.Resolve<LPCore>();
            core.Start();

            ThreadCore.Root.Sleep(1000);

            core.Terminate();
        }

        public Container Container { get; }
    }
}
