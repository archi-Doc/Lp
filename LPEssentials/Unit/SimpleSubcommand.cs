// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace LP.Unit;

public abstract class SimpleSubcommand<TCommand> : ISimpleCommandAsync
    where TCommand : SimpleSubcommand<TCommand>
{
    public static CommandGroup ConfigureGroup(UnitBuilderContext context, Type? parentCommand = null)
    {
        parentCommand ??= typeof(object);
        var commandType = typeof(TCommand);

        // Add a command type to the parent.
        var group = context.GetCommandGroup(parentCommand);
        group.AddCommand(commandType);

        // Get the command group.
        group = context.GetCommandGroup(commandType);
        return group;
    }

    public SimpleSubcommand(UnitParameter parameter, string? defaultArgument = null)
    {
        this.commandTypes = parameter.GetCommandTypes(typeof(TCommand));
        this.SimpleParserOptions = SimpleParserOptions.Standard with
        {
            ServiceProvider = parameter.ServiceProvider,
            RequireStrictCommandName = true,
            RequireStrictOptionName = true,
            DoNotDisplayUsage = true,
            DisplayCommandListAsHelp = true,
        };

        this.defaultArgument = defaultArgument;
    }

    public async Task Run(string[] args)
    {
        if (args.Length == 0 && this.defaultArgument != null)
        {// default argument
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
