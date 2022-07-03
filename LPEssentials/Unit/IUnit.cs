// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace LP.Unit;

public interface IUnit
{
    public void Configure();

    public Task LoadAsync();

    public Task StartAsync(ThreadCoreBase parentCore);

    public Task SaveAsync();

    public Task TerminateAsync();
}
