// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using SimpleCommandLine;
using SimplePrompt;

namespace Lp.Subcommands;

public class NestedCommand<TCommand>
    where TCommand : NestedCommand<TCommand>
{
    private readonly ExecutionStack executionStack;
    private readonly SimpleConsole simpleConsole;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly Type[] commandTypes;
    private SimpleParser? simpleParser;

    public NestedCommand(UnitContext context)
    {
        this.executionStack = context.ServiceProvider.GetRequiredService<ExecutionStack>();
        this.simpleConsole = context.ServiceProvider.GetRequiredService<SimpleConsole>();
        this.userInterfaceService = context.ServiceProvider.GetRequiredService<IUserInterfaceService>();

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

        using (var scope = this.executionStack.Push((x, signal) =>
        {
            if (signal == ExecutionSignal.Exit)
            {
                x.CancellationTokenSource.Cancel();
            }
        }))
        {
            while (scope.CanContinue)
            {//
                var result = await this.simpleConsole.ReadLine(this.ReadLineOptions, scope.CancellationToken).ConfigureAwait(false);
                if (!result.IsSuccess)
                {
                    break;
                }

                if (string.Compare(result.Text, "exit", true) == 0)
                {// Exit
                    break;
                }

                // NestedCommand
                try
                {
                    if (this.SimpleParser.Parse(result.Text))
                    {
                        using (var scope2 = this.executionStack.Push((x, signal) =>
                        {
                            if (signal == ExecutionSignal.Cancel)
                            {
                                x.CancellationTokenSource.Cancel();
                                this.userInterfaceService.WriteLineError(Hashed.Dialog.Canceled);
                            }
                        }))
                        {
                            await this.SimpleParser.Execute(scope2.CancellationToken);
                        }
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

        this.userInterfaceService.WriteLineError(Hashed.Dialog.Exit);
        await Task.Delay(LpParameters.ExitDelayMilliseconds);
    }
}
