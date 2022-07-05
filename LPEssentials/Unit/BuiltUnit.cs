// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace LP.Unit;

/*public class BuiltUnit<T> : BuiltUnit
{
    public BuiltUnit(UnitBuilderContext context)
        : base(context)
    {
    }
}*/

public class BuiltUnit : UnitBase
{
    public BuiltUnit(UnitBuilderContext context)
        : base(controlUnit)
    {
        this.ServiceProvider = context.ServiceCollection.BuildServiceProvider();
        this.commandTypes = context.CommandCollection.Select(a => a.CommandType).ToArray();
    }

    public void Run()
    {
    }

    public void Configure()
    {
    }

    public async Task LoadAsync()
    {
    }

    public async Task SaveAsync()
    {
    }

    public async Task StartAsync(ThreadCoreBase parentCore)
    {
    }

    public async Task TerminateAsync()
    {
    }

    public ServiceProvider ServiceProvider { get; init; }

    public IEnumerable<Type> CommandTypes
    {
        get
        {
            foreach (var x in this.commandTypes)
            {
                yield return x;
            }
        }
    }

    private Type[] commandTypes;
}
