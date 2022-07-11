// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace Arc.Unit;

public abstract class SimpleSubcommand<TCommand> : ISimpleCommandAsync
    where TCommand : SimpleSubcommand<TCommand>
{
    public static CommandGroup ConfigureGroup(UnitBuilderContext context, Type? parentCommand = null)
    {
        var commandType = typeof(TCommand);

        // Add a command type to the parent.
        CommandGroup group;
        if (parentCommand != null)
        {
            group = context.GetCommandGroup(parentCommand);
        }
        else
        {
            group = context.GetSubcommandGroup();
        }

        group.AddCommand(commandType);

        // Get the command group.
        group = context.GetCommandGroup(commandType);
        return group;
    }

    public SimpleSubcommand(UnitContext context, string? defaultArgument = null, SimpleParserOptions? parserOptions = null)
    {
        this.commandTypes = context.GetCommandTypes(typeof(TCommand));

        if (parserOptions != null)
        {
            this.SimpleParserOptions = parserOptions with { ServiceProvider = context.ServiceProvider, };
        }
        else
        {
            this.SimpleParserOptions = SimpleParserOptions.Standard with
            {
                ServiceProvider = context.ServiceProvider,
                RequireStrictCommandName = true,
                RequireStrictOptionName = true,
                DoNotDisplayUsage = true,
                DisplayCommandListAsHelp = true,
            };
        }

        this.defaultArgument = defaultArgument;
    }

    public async Task Run(string[] args)
    {
        if (args.Length == 0 && this.defaultArgument != null)
        {// Default argument
            args = new string[] { this.defaultArgument, };
        }

        await this.SimpleParser.ParseAndRunAsync(args).ConfigureAwait(false);
    }

    public SimpleParserOptions SimpleParserOptions { get; }

    public SimpleParser SimpleParser
    {
        get
        {
            this.simpleParser ??= new(this.commandTypes, this.SimpleParserOptions);
            return this.simpleParser;
        }
    }

    private Type[] commandTypes;
    private SimpleParser? simpleParser;
    private string? defaultArgument;
}
