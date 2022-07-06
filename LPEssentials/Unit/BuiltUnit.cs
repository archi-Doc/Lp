// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using CrossChannel;
using LPEssentials.Radio;
using Microsoft.Extensions.DependencyInjection;

namespace LP.Unit;

public class BuiltUnit : UnitBase, IUnitExecutable, IUnitSerializable
{
    public BuiltUnit(UnitParameter parameter)
        : base(parameter)
    {
        this.ServiceProvider = parameter.ServiceProvider;
        this.radio = parameter.Radio;
        this.commandTypes = parameter.CommandTypes;
    }

    /*public void Run()
    {
    }*/

    public override void Configure(UnitMessage.Configure message)
        => this.radio.SendAsync(message).ConfigureAwait(false);

    public async Task LoadAsync(UnitMessage.LoadAsync message)
        => await this.radio.SendAsync(message).ConfigureAwait(false);

    public async Task SaveAsync(UnitMessage.SaveAsync message)
        => await this.radio.SendAsync(message).ConfigureAwait(false);

    public async Task RunAsync(UnitMessage.RunAsync message)
        => await this.radio.SendAsync(message).ConfigureAwait(false);

    public async Task TerminateAsync(UnitMessage.TerminateAsync message)
        => await this.radio.SendAsync(message).ConfigureAwait(false);

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

    private RadioClass radio;
    private Type[] commandTypes;

    internal void AddInternal(UnitBase unit)
    {
        this.radio.Open<UnitMessage.Configure>(x => unit.Configure(x), unit);

        if (unit is IUnitExecutable executable)
        {
            this.radio.Open<UnitMessage.RunAsync>(x => executable.RunAsync(x), unit);
            this.radio.Open<UnitMessage.TerminateAsync>(x => executable.TerminateAsync(x), unit);
        }

        if (unit is IUnitSerializable serializable)
        {
            this.radio.Open<UnitMessage.LoadAsync>(x => serializable.LoadAsync(x), unit);
            this.radio.Open<UnitMessage.SaveAsync>(x => serializable.SaveAsync(x), unit);
        }
    }
}
