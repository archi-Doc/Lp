// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using SimpleCommandLine;

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

    public string Prompt { get; protected set; } = "> ";

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

    public async Task MainAsync(CancellationToken cancellationToken)
    {
        var parent = cancellationToken.ExtractCore();
        if (parent is null)
        {
            return;
        }

        using (var executionContext = this.executionStack.PushNew(parent, (x, signal) =>
        {
            if (signal == ExecutionSignal.Exit)
            {
                x.RequestTermination();
            }
        }))
        {
            while (executionContext.CanContinue)
            {
                var result = await this.userInterfaceService.ReadLine(false, this.Prompt, executionContext.Token).ConfigureAwait(false);
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
                        using (var executionContext2 = this.executionStack.PushNew(executionContext, (x, signal) =>
                        {
                            if (signal == ExecutionSignal.Cancel)
                            {
                                x.RequestTermination();
                                this.userInterfaceService.WriteLineError(Hashed.Dialog.Canceled);
                            }
                        }))
                        {
                            await this.SimpleParser.Execute(executionContext2.Token);
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
