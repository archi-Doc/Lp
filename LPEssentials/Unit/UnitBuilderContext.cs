// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arc.Unit;

/// <summary>
/// Contextual information provided to <see cref="UnitBuilder"/>.<br/>
/// </summary>
internal class UnitBuilderContext : IUnitPreloadContext, IUnitConfigurationContext, IUnitSetupContext
{
    private const string RootDirectoryOption = "root";
    private const string DataDirectoryOption = "data";

    public UnitBuilderContext()
    {
        this.UnitName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
        this.RootDirectory = string.Empty;
        this.DataDirectory = string.Empty;
    }

    /// <summary>
    /// Gets or sets a unit name.
    /// </summary>
    public string UnitName { get; set; }

    /// <summary>
    /// Gets or sets a root directory.
    /// </summary>
    public string RootDirectory { get; set; }

    /// <summary>
    /// Gets or sets a data directory.
    /// </summary>
    public string DataDirectory { get; set; }

    /// <summary>
    /// Gets command-line arguments.
    /// </summary>
    public UnitArguments Arguments => this.arguments;

    /// <summary>
    /// Gets <see cref="IServiceCollection"/>.
    /// </summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

#pragma warning disable CS8766
    public IServiceProvider? ServiceProvider { get; internal set; }
#pragma warning restore CS8766

    internal HashSet<Type> CreateInstanceSet { get; } = new();

    internal Dictionary<Type, CommandGroup> CommandGroups { get; } = new();

    internal List<LoggerResolverDelegate> LoggerResolvers { get; } = new();

    public void ClearLoggerResolver() => this.LoggerResolvers.Clear();

    public void AddLoggerResolver(LoggerResolverDelegate resolver) => this.LoggerResolvers.Add(resolver);

    public void CreateInstance<T>()
        => this.CreateInstanceSet.Add(typeof(T));

    public CommandGroup GetCommandGroup(Type type)
    {
        if (!this.CommandGroups.TryGetValue(type, out var commandGroup))
        {
            this.TryAddSingleton(type);
            commandGroup = new(this);
            this.CommandGroups.Add(type, commandGroup);
        }

        return commandGroup;
    }

    public CommandGroup GetCommandGroup() => this.GetCommandGroup(typeof(TopCommand));

    public CommandGroup GetSubcommandGroup() => this.GetCommandGroup(typeof(SubCommand));

    public bool AddCommand(Type commandType)
    {
        var group = this.GetCommandGroup();
        return group.AddCommand(commandType);
    }

    public bool AddSubcommand(Type commandType)
    {
        var group = this.GetSubcommandGroup();
        return group.AddCommand(commandType);
    }

    internal class TopCommand
    {
    }

    internal class SubCommand
    {
    }

    internal void SetDirectory()
    {
        if (this.arguments.TryGetOption(RootDirectoryOption, out var value))
        {// Root Directory
            if (Path.IsPathRooted(value))
            {
                this.RootDirectory = value;
            }
            else
            {
                this.RootDirectory = Path.Combine(Directory.GetCurrentDirectory(), value);
            }
        }
        else
        {
            this.RootDirectory = Directory.GetCurrentDirectory();
        }

        if (this.arguments.TryGetOption(DataDirectoryOption, out value))
        {// Data Directory
            if (Path.IsPathRooted(value))
            {
                this.DataDirectory = value;
            }
            else
            {
                this.DataDirectory = Path.Combine(Directory.GetCurrentDirectory(), value);
            }
        }
    }

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401
    internal UnitArguments arguments = new();
#pragma warning restore SA1401
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter
}
