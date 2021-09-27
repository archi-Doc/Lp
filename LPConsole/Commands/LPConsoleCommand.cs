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
        public LPConsoleCommand()
        {
        }

        public async Task Run(LPConsoleOptions option, string[] args)
        {
            var info = Program.Container.Resolve<Information>();
            info.Configure(option, true, "relay");

            var control = Program.Container.Resolve<Control>();
            control.Configure();
            control.Start();
            control.BigMachine.TryCreate<LP.Machines.SingleMachine.Interface>(Identifier.Zero);

            control.MainLoop();

            control.Terminate();
        }
    }
}
