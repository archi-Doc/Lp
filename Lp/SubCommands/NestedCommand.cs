// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Lp.Subcommands;

public class NestedCommand<TCommand>
    where TCommand : NestedCommand<TCommand>
{
    public NestedCommand(UnitContext context, UnitCore core, IUserInterfaceService userInterfaceService)
    {
        this.Core = core;
        this.userInterfaceService = userInterfaceService;

        this.commandTypes = context.GetCommandTypes(typeof(TCommand));
        this.SimpleParserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = context.ServiceProvider,
            RequireStrictCommandName = true,
            RequireStrictOptionName = true,
            DoNotDisplayUsage = true,
            DisplayCommandListAsHelp = true,
            AutoAlias = true,
        };
    }

    /// <summary>
    /// Gets <see cref="UnitCore"/>.
    /// </summary>
    public UnitCore Core { get; }

    /// <summary>
    /// Gets <see cref="SimpleParserOptions"/>.
    /// </summary>
    public SimpleParserOptions SimpleParserOptions { get; }

    /// <summary>
    /// Gets <see cref="SimpleParser"/> instance.
    /// </summary>
    public SimpleParser SimpleParser
    {
        get
        {
            this.simpleParser ??= new(this.commandTypes, this.SimpleParserOptions);
            return this.simpleParser;
        }
    }

    public virtual string Prefix { get; set; } = ">> ";

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
                    return this.userInterfaceService.ReadLine().Text?.Trim();
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
                {// NestedCommand
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
                return;
            }
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

    private readonly IUserInterfaceService userInterfaceService;
    private readonly Type[] commandTypes;
    private SimpleParser? simpleParser;
}
