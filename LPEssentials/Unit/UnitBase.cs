// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace LP.Unit;

/// <summary>
/// Base class of Unit.<br/>
/// Unit is an independent unit of function and dependency.
/// </summary>
public abstract class UnitBase
{
    public UnitBase(UnitParameter parameter)
    {
    }

    /*public UnitBase(BuiltUnit? builtUnit)
    {
        this.BuiltUnit = builtUnit;
        this.BuiltUnit?.AddInternal(this);
    }*/

    public virtual void Configure(UnitMessage.Configure message)
    {
    }

    // public BuiltUnit? BuiltUnit { get; }
}

/*public interface IUnitConfigurable
{
    public void Configure();
}*/

public interface IUnitExecutable
{
    public Task RunAsync(UnitMessage.RunAsync message);

    public Task TerminateAsync(UnitMessage.TerminateAsync message);
}

public interface IUnitSerializable
{
    public Task LoadAsync(UnitMessage.LoadAsync message);

    public Task SaveAsync(UnitMessage.SaveAsync message);
}
