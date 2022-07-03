// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace LP.Unit;

public class BuiltUnit : IUnit
{
    public BuiltUnit(ServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
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

    private ServiceProvider serviceProvider;
}
