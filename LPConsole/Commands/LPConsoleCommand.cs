// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using DryIoc;
using LP;
using SimpleCommandLine;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace LPConsole;

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
        await control.LoadAsync();
        if (!control.TryStart())
        {
            Logger.Default.Information("LP Aborted");
            return;
        }

        this.MainLoop(control);

        control.Stop();
        await control.SaveAsync();
        control.Terminate();
    }

    private void MainLoop(Control control)
    {
        while (!control.Core.IsTerminated)
        {
            if (Logger.ViewMode)
            {// View mode
                if (this.SafeKeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);
                    if (keyInfo.Key == ConsoleKey.Enter || keyInfo.Key == ConsoleKey.Escape)
                    { // To console mode
                        Logger.ViewMode = false;
                        Console.Write("> ");
                    }
                }
            }
            else
            {// Console mode
                var command = Console.ReadLine();
                if (!string.IsNullOrEmpty(command))
                {
                    if (string.Compare(command, "exit", true) == 0)
                    {// Exit
                        return;
                    }
                    else
                    {
                    }
                }

                // To view mode
                Logger.ViewMode = true;
            }

            control.Core.Sleep(100, 100);
        }
    }

    private bool SafeKeyAvailable
    {
        get
        {
            try
            {
                return Console.KeyAvailable;
            }
            catch
            {
                return false;
            }
        }
    }
}
