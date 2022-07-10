// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using CrossChannel;
using LPEssentials.Radio;
using Microsoft.Extensions.DependencyInjection;

namespace LP.Unit;

public class BuiltUnit : UnitBase
{
    public BuiltUnit(UnitContext context)
        : base(context)
    {
        this.ServiceProvider = context.ServiceProvider;
        this.radio = context.Radio;
        this.commandTypes = context.CommandTypes;
        this.createInstanceTypes = context.CreateInstanceTypes;
    }

    /*public void Run()
    {
    }*/

    public void SendPrepare(UnitMessage.Prepare message)
        => this.radio.Send(message);

    public async Task SendLoadAsync(UnitMessage.LoadAsync message)
        => await this.radio.SendAsync(message).ConfigureAwait(false);

    public async Task SendSaveAsync(UnitMessage.SaveAsync message)
        => await this.radio.SendAsync(message).ConfigureAwait(false);

    public async Task SendRunAsync(UnitMessage.RunAsync message)
        => await this.radio.SendAsync(message).ConfigureAwait(false);

    public async Task SendTerminateAsync(UnitMessage.TerminateAsync message)
        => await this.radio.SendAsync(message).ConfigureAwait(false);

    public void CreateInstances()
    {
        foreach (var x in this.createInstanceTypes)
        {
            this.ServiceProvider.GetService(x);
        }
    }

    public IServiceProvider ServiceProvider { get; init; }

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
    private Type[] createInstanceTypes;

    internal void AddInternal(UnitBase unit)
    {
        if (unit is IUnitPreparable configurable)
        {
            this.radio.Open<UnitMessage.Prepare>(x => configurable.Prepare(x), unit);
        }

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
