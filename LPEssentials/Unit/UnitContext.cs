// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Contextual information provided to Unit.<br/>
/// Objects managed by Unit should not make direct use of <see cref="UnitBuilderContext"/>.
/// </summary>
public class UnitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitContext"/> class.
    /// </summary>
    public UnitContext()
    {
    }

    /// <summary>
    /// Converts <see cref="UnitBuilderContext"/> to <see cref="UnitContext"/>.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceCollection"/>.</param>
    /// <param name="builderContext"><see cref="UnitBuilderContext"/>.</param>
    public void FromBuilderContext(IServiceProvider serviceProvider, UnitBuilderContext builderContext)
    {
        this.ServiceProvider = serviceProvider;
        this.Radio = serviceProvider.GetRequiredService<RadioClass>();
        this.CreateInstanceTypes = builderContext.CreateInstanceSet.ToArray();
        this.CommandTypes = builderContext.CommandList.ToArray();

        foreach (var x in builderContext.CommandGroups)
        {
            this.Subcommands[x.Key] = x.Value.ToArray();
        }
    }

    /// <summary>
    /// Gets an array of command <see cref="Type"/> which belong to the specified command type.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns>An array of command type.</returns>
    public Type[] GetCommandTypes(Type commandType)
    {
        if (this.Subcommands.TryGetValue(commandType, out var array))
        {
            return array;
        }
        else
        {
            return Array.Empty<Type>();
        }
    }

    /// <summary>
    /// Gets an instance of <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; } = default!;

    /// <summary>
    /// Gets an instance of <see cref="RadioClass"/>.
    /// </summary>
    public RadioClass Radio { get; private set; } = default!;

    /// <summary>
    /// Gets an array of <see cref="Type"/> which is registered in the creation list.<br/>
    /// Note that instances are actually created by calling <see cref="BuiltUnit.CreateInstances()"/>.
    /// </summary>
    public Type[] CreateInstanceTypes { get; private set; } = default!;

    /// <summary>
    /// Gets an array of command <see cref="Type"/>.
    /// </summary>
    public Type[] CommandTypes { get; private set; } = default!;

    /// <summary>
    /// Gets a collection of command <see cref="Type"/> (keys) and subcommand <see cref="Type"/> (values).
    /// </summary>
    public Dictionary<Type, Type[]> Subcommands { get; private set; } = new();
}
