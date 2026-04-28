// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Lp.NetServices;
using Lp.Services;
using Microsoft.Extensions.DependencyInjection;
using SimpleCommandLine;
using SimplePrompt;

namespace Lp.Subcommands;

public class NestedCommand<TCommand>
    where TCommand : NestedCommand<TCommand>
{
    private readonly IServiceProvider serviceProvider;
    private readonly ExecutionStack executionStack;
    private readonly IUserInterfaceService userInterfaceService;
    private readonly Type[] commandTypes;

    public SimpleParserOptions SimpleParserOptions { get; }

    public SimpleParser SimpleParser { get; }

    public ReadLineOptions? ReadLineOptions { get; protected set; }

    public NestedCommand(UnitContext context, IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        this.executionStack = this.serviceProvider.GetRequiredService<ExecutionStack>();

        this.commandTypes = context.GetCommandTypes(typeof(TCommand));
        this.SimpleParserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = this.serviceProvider,
            RequireStrictCommandName = true,
            RequireStrictOptionName = true,
            DoNotDisplayUsage = true,
            DisplayCommandListAsHelp = true,
            AutoAlias = true,
        };

        // this.serviceProvider.GetRequiredService<UserInterfaceServiceContext>().InitializeLocal();
        this.SimpleParser = new SimpleParser(this.commandTypes, this.SimpleParserOptions);
        this.userInterfaceService = this.serviceProvider.GetRequiredService<IUserInterfaceService>();
    }

    public async Task MainAsync()
    {
        this.ReadLineOptions ??= new ReadLineOptions
        {
            Prompt = "> ",
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
                // var result = await this.simpleConsole.ReadLine(this.ReadLineOptions, scope.CancellationToken).ConfigureAwait(false);
                var result = await this.userInterfaceService.ReadLine(false, this.ReadLineOptions.Prompt, scope.CancellationToken).ConfigureAwait(false);
                if (!result.IsSuccess)
                {// Canceled, Terminated, Timeout
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
                            this.userInterfaceService.WriteLine("Invalid subcommand.");
                        }
                    }

                    continue;
                }
                catch (Exception e)
                {
                    this.userInterfaceService.WriteLine(e.ToString());
                    break;
                }
            }
        }

        this.userInterfaceService.WriteLineError(Hashed.Dialog.Exit);
        await Task.Delay(LpParameters.ExitDelayMilliseconds);
    }
}
