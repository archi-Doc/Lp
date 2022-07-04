// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Unit;

public class CommandDescriptor
{
    public CommandDescriptor(Type commandType)
    {
        this.CommandType = commandType;
    }

    public Type CommandType { get; }
}
