// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using CrossChannel;
using LPEssentials.Radio;
using Microsoft.Extensions.DependencyInjection;

namespace LP.Unit;

public class BuiltUnit : UnitBase
{
    public BuiltUnit(UnitParameter parameter)
        : base(parameter)
    {
        this.ServiceProvider = parameter.ServiceProvider;
        this.radio = parameter.Radio;
        this.commandTypes = parameter.CommandTypes;
        this.createInstanceTypes = parameter.CreateInstanceTypes;
    }

    /*public void Run()
    {
    }*/

    public void SendConfigure(UnitMessage.Configure message)
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
        if (unit is IUnitConfigurable configurable)
        {
            this.radio.Open<UnitMessage.Configure>(x => configurable.Configure(x), unit);
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
