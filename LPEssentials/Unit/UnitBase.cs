// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Base class of Unit.<br/>
/// Unit is an independent unit of function and dependency.<br/>
/// By implementing <see cref="IUnitPreparable"/> and other interfaces, methods can be called from <see cref="UnitContext"/>.
/// </summary>
public abstract class UnitBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitBase"/> class.
    /// </summary>
    /// <param name="context"><see cref="UnitContext"/>.</param>
    public UnitBase(UnitContext context)
    {
        context.AddRadio(this);
    }
}

public interface IUnitPreparable
{
    public void Prepare(UnitMessage.Prepare message);
}

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
