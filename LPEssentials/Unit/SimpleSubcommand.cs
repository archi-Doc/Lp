// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace Arc.Unit;

/// <summary>
/// <see cref="SimpleSubcommand{TCommand}"/> is base class for a collection of subcommands.
/// </summary>
/// <typeparam name="TCommand">The type of command class.</typeparam>
public abstract class SimpleSubcommand<TCommand> : ISimpleCommandAsync
    where TCommand : SimpleSubcommand<TCommand>
{
    /// <summary>
    /// Gets a <see cref="CommandGroup"/> to configure subcommands.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="parentCommand"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleSubcommand{TCommand}"/> class.
    /// </summary>
    /// <param name="context"><see cref="UnitContext"/>.</param>
    /// <param name="defaultArgument">The default argument to be used if the argument is empty.</param>
    /// <param name="parserOptions"><see cref="SimpleParserOptions"/>.</param>
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

    /// <summary>
    /// Parse the arguments and executes the specified command.<br/>
    /// The default argument will be used if the argument is empty.
    /// </summary>
    /// <param name="args">The arguments to specify commands and options.</param>
    /// <returns><see cref="Task"/>.</returns>
    public async Task Run(string[] args)
    {
        if (args.Length == 0 && this.defaultArgument != null)
        {// Default argument
            args = new string[] { this.defaultArgument, };
        }

        await this.SimpleParser.ParseAndRunAsync(args).ConfigureAwait(false);
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

    private Type[] commandTypes;
    private SimpleParser? simpleParser;
    private string? defaultArgument;
}
