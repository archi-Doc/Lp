// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace LP.Unit;

/// <summary>
/// Unit of function and dependency.
/// </summary>
public interface IUnit
{
}

public interface IUnitConfigurable : IUnit
{
    public void Configure();
}

public interface IUnitExecutable : IUnit
{
    public Task StartAsync(ThreadCoreBase parentCore);

    public Task TerminateAsync();
}

public interface IUnitSerializable : IUnit
{
    public Task LoadAsync();

    public Task SaveAsync();
}
