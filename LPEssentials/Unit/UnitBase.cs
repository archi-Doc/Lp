// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Base class of Unit.<br/>
/// Unit is an independent unit of function and dependency.
/// </summary>
public abstract class UnitBase
{
    public UnitBase(UnitContext context)
    {
        var radio = context.Radio;

        if (this is IUnitPreparable configurable)
        {
            radio.Open<UnitMessage.Prepare>(x => configurable.Prepare(x), this);
        }

        if (this is IUnitExecutable executable)
        {
            radio.OpenAsync<UnitMessage.RunAsync>(x => executable.RunAsync(x), this);
            radio.OpenAsync<UnitMessage.TerminateAsync>(x => executable.TerminateAsync(x), this);
        }

        if (this is IUnitSerializable serializable)
        {
            radio.OpenAsync<UnitMessage.LoadAsync>(x => serializable.LoadAsync(x), this);
            radio.OpenAsync<UnitMessage.SaveAsync>(x => serializable.SaveAsync(x), this);
        }
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
