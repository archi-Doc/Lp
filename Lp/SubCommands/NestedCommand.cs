// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;
using SimplePrompt;

namespace Lp.Subcommands;

public class NestedCommand<TCommand>
    where TCommand : NestedCommand<TCommand>
{
    public NestedCommand(UnitContext context, UnitCore core, SimpleConsole simpleConsole)
    {
        this.Core = core;
        this.simpleConsole = simpleConsole;

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

    public ReadLineOptions? ReadLineOptions { get; protected set; }

    public async Task MainAsync()
    {
        this.ReadLineOptions ??= new ReadLineOptions
        {
            Prompt = ">> ",
            MultilinePrompt = LpConstants.MultilinePromptString,
        };

        while (!this.Core.IsTerminated)
        {
            var result = await this.simpleConsole.ReadLine(this.ReadLineOptions, this.Core.CancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                if (string.Compare(result.Text, "exit", true) == 0)
                {// Exit
                    return;
                }
                else
                {// NestedCommand
                    try
                    {
                        if (this.SimpleParser.Parse(result.Text))
                        {
                            this.SimpleParser.Run();
                        }
                        else
                        {
                            if (this.SimpleParser.HelpCommand != string.Empty)
                            {
                                this.SimpleParser.ShowHelp();
                            }
                            else
                            {
                                this.simpleConsole.WriteLine("Invalid subcommand.");
                            }
                        }

                        continue;
                    }
                    catch (Exception e)
                    {
                        this.simpleConsole.WriteLine(e.ToString());
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

    private readonly SimpleConsole simpleConsole;
    private readonly Type[] commandTypes;
    private SimpleParser? simpleParser;
}
