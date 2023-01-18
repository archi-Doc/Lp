// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace LP.Subcommands;

public class Nestedcommand<TCommand>
    where TCommand : Nestedcommand<TCommand>
{
    public Nestedcommand(UnitContext context, UnitCore core, IUserInterfaceService userInterfaceService)
        : base(context)
    {
        this.Core = core;
        this.userInterfaceService = userInterfaceService;
    }

    public UnitCore Core { get; }

    public string Prefix { get; set; } = ">> ";

    public async Task MainAsync()
    {
        while (!this.Core.IsTerminated)
        {
            this.userInterfaceService.Write(this.Prefix);

            string? command = null;
            try
            {
                command = await Task.Run(() =>
                {
                    return this.userInterfaceService.ReadLine()?.Trim();
                }).WaitAsync(this.Core.CancellationToken).ConfigureAwait(false);
            }
            catch
            {
            }

            if (!string.IsNullOrEmpty(command))
            {
                if (string.Compare(command, "exit", true) == 0)
                {// Exit
                    return;
                }
                else
                {// Nestedcommand
                    try
                    {
                        this.Subcommand(command);
                        continue;
                    }
                    catch (Exception e)
                    {
                        this.userInterfaceService.WriteLine(e.ToString());
                        break;
                    }
                }
            }
            else
            {
                this.userInterfaceService.WriteLine();
            }

            this.Core.Sleep(100, 100);
        }
    }

    public bool Subcommand(string subcommand)
    {
        if (!this.SimpleParser.Parse(subcommand))
        {
            if (this.SimpleParser.HelpCommand != string.Empty)
            {
                this.SimpleParser.ShowHelp();
                return true;
            }
            else
            {
                this.userInterfaceService.WriteLine("Invalid subcommand.");
                return false;
            }
        }

        this.SimpleParser.Run();
        return true;

        /*if (subcommandParser.HelpCommand != string.Empty)
        {
            return false;
        }

        this.ConsoleService.WriteLine();
        return true;*/
    }

    private IUserInterfaceService userInterfaceService;
}
