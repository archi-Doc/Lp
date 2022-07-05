// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace LP.Unit;

/// <summary>
/// Unit of function and dependency.
/// </summary>
public abstract class UnitBase
{
    public UnitBase(BuiltUnit controlUnit)
    {
        this.BuiltUnit = controlUnit;
    }

    public BuiltUnit BuiltUnit { get; }
}

public interface IUnitConfigurable
{
    public void Configure();
}

public interface IUnitExecutable
{
    public Task StartAsync(ThreadCoreBase parentCore);

    public Task TerminateAsync();
}

public interface IUnitSerializable
{
    public Task LoadAsync();

    public Task SaveAsync();
}
