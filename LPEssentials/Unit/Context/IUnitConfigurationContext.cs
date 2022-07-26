// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Contextual information used by Configuration delegate and provided to <see cref="UnitBuilder"/>.
/// </summary>
public interface IUnitConfigurationContext : IUnitPreloadContext
{
    /// <summary>
    /// Gets <see cref="IServiceCollection"/>.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Adds a logger resolver which determines appropriate <see cref="ILogOutput"/> and <see cref="ILogFilter"/> from Log source and <see cref="LogLevel"/>.
    /// </summary>
    /// <param name="resolver"><see cref="LoggerResolverDelegate"/>.</param>
    public void AddLoggerResolver(LoggerResolverDelegate resolver);

    public void ClearLoggerResolver();

    /// <summary>
    /// Adds the specified <see cref="Type"/> to the creation list.
    /// Note that instances are actually created by calling <see cref="UnitContext.CreateInstances()"/>.
    /// </summary>
    /// <typeparam name="T">The type to be instantiated.</typeparam>
    public void CreateInstance<T>();

    /// <summary>
    /// Gets <see cref="CommandGroup"/> of the specified command type.
    /// </summary>
    /// <param name="type">The command type.</param>
    /// <returns><see cref="CommandGroup"/>.</returns>
    public CommandGroup GetCommandGroup(Type type);

    /// <summary>
    /// Gets <see cref="CommandGroup"/> of command.
    /// </summary>
    /// <returns><see cref="CommandGroup"/>.</returns>
    public CommandGroup GetCommandGroup();

    /// <summary>
    /// Gets <see cref="CommandGroup"/> of subcommand.
    /// </summary>
    /// <returns><see cref="CommandGroup"/>.</returns>
    public CommandGroup GetSubcommandGroup();

    /// <summary>
    /// Adds command.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns><see langword="true"/>: Successfully added.</returns>
    public bool AddCommand(Type commandType);

    /// <summary>
    /// Adds subcommand.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns><see langword="true"/>: Successfully added.</returns>
    public bool AddSubcommand(Type commandType);
}
