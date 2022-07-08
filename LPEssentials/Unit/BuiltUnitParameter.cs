// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace LP.Unit;

public class UnitParameter
{
    public UnitParameter()
    {
    }

    public void FromContext(IServiceProvider serviceProvider, UnitBuilderContext context)
    {
        this.ServiceProvider = serviceProvider;
        this.Radio = serviceProvider.GetRequiredService<RadioClass>();
        this.CommandTypes = context.CommandList.ToArray();

        foreach (var x in context.CommandGroups)
        {
            this.Subcommands[x.Key] = x.Value.ToArray();
        }
    }

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

    public IServiceProvider ServiceProvider { get; private set; } = default!;

    public RadioClass Radio { get; private set; } = default!;

    public Type[] CommandTypes { get; private set; } = default!;

    public Dictionary<Type, Type[]> Subcommands { get; private set; } = new();
}
