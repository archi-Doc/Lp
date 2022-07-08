// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace LP.Unit;

public abstract class SimpleSubcommand<TCommand> : ISimpleCommandAsync
    where TCommand : SimpleSubcommand<TCommand>
{
    public class Builder : UnitBuilder
    {
        public Builder(Type? parentCommand)
        {
            this.Configure(context =>
            {
                var group = context.GetCommandGroup(typeof(TCommand));
            });
        }
    }

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

    public SimpleSubcommand(UnitParameter parameter)
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
    }

    public async Task Run(string[] args)
    {
        if (this.subcommandParser == null)
        {
            this.subcommandParser ??= new(this.commandTypes, this.SimpleParserOptions);
        }

        await this.subcommandParser.ParseAndRunAsync(args);
    }

    public SimpleParserOptions SimpleParserOptions { get; }

    private Type[] commandTypes;
    private SimpleParser? subcommandParser;
}
