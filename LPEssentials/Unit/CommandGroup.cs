// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// <see cref="CommandGroup"/> is a collection of command types.
/// </summary>
public class CommandGroup
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandGroup"/> class.
    /// </summary>
    /// <param name="context"><see cref="UnitBuilderContext"/>.</param>
    public CommandGroup(UnitBuilderContext context)
    {
        this.context = context;
    }

    /// <summary>
    /// Adds a command type to the <see cref="CommandGroup"/>.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns><see langword="true"/>: Successfully added.</returns>
    public bool AddCommand(Type commandType)
    {
        if (this.commandSet.Contains(commandType))
        {
            return false;
        }
        else
        {
            this.context.TryAddSingleton(commandType);
            this.commandSet.Add(commandType);
            this.commandList.Add(commandType);
            return true;
        }
    }

    /// <summary>
    /// Gets an array of command types.
    /// </summary>
    /// <returns>An array of <see cref="Type"/>.</returns>
    public Type[] ToArray() => this.commandList.ToArray();

    private UnitBuilderContext context;
    private List<Type> commandList = new();
    private HashSet<Type> commandSet = new();
}
