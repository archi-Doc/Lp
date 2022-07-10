// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Unit;

public class CommandGroup
{
    public CommandGroup(UnitBuilderContext context)
    {
        this.context = context;
    }

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

    public Type[] ToArray() => this.commandList.ToArray();

    private UnitBuilderContext context;
    private List<Type> commandList = new();
    private HashSet<Type> commandSet = new();
}
