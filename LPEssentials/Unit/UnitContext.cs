// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Contextual information provided to <see cref="UnitBase"/>.<br/>
/// In terms of DI, you should avoid using <see cref="UnitContext"/> if possible.
/// </summary>
public sealed class UnitContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitContext"/> class.
    /// </summary>
    public UnitContext()
    {
    }

    /// <summary>
    /// Converts <see cref="UnitBuilderContext"/> to <see cref="UnitContext"/>.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceCollection"/>.</param>
    /// <param name="builderContext"><see cref="UnitBuilderContext"/>.</param>
    public void FromBuilderContext(IServiceProvider serviceProvider, UnitBuilderContext builderContext)
    {
        this.ServiceProvider = serviceProvider;
        this.Radio = serviceProvider.GetRequiredService<RadioClass>();
        this.CreateInstanceTypes = builderContext.CreateInstanceSet.ToArray();

        builderContext.GetCommandGroup(typeof(UnitBuilderContext.TopCommand));
        builderContext.GetCommandGroup(typeof(UnitBuilderContext.SubCommand));
        foreach (var x in builderContext.CommandGroups)
        {
            this.CommandDictionary[x.Key] = x.Value.ToArray();
        }
    }

    /// <summary>
    /// Gets an array of command <see cref="Type"/> which belong to the specified command type.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns>An array of command type.</returns>
    public Type[] GetCommandTypes(Type commandType)
    {
        if (this.CommandDictionary.TryGetValue(commandType, out var array))
        {
            return array;
        }
        else
        {
            return Array.Empty<Type>();
        }
    }

    /// <summary>
    /// Create instances registered by <see cref="UnitBuilderContext.CreateInstance{T}()"/>.
    /// </summary>
    public void CreateInstances()
    {
        foreach (var x in this.CreateInstanceTypes)
        {
            this.ServiceProvider.GetService(x);
        }
    }

    public void SendPrepare(UnitMessage.Prepare message)
        => this.Radio.Send(message);

    public async Task SendRunAsync(UnitMessage.RunAsync message)
        => await this.Radio.SendAsync(message).ConfigureAwait(false);

    public async Task SendTerminateAsync(UnitMessage.TerminateAsync message)
        => await this.Radio.SendAsync(message).ConfigureAwait(false);

    public async Task SendLoadAsync(UnitMessage.LoadAsync message)
        => await this.Radio.SendAsync(message).ConfigureAwait(false);

    public async Task SendSaveAsync(UnitMessage.SaveAsync message)
        => await this.Radio.SendAsync(message).ConfigureAwait(false);

    /// <summary>
    /// Gets an instance of <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; } = default!;

    /// <summary>
    /// Gets an instance of <see cref="RadioClass"/>.
    /// </summary>
    public RadioClass Radio { get; private set; } = default!;

    /// <summary>
    /// Gets an array of <see cref="Type"/> which is registered in the creation list.<br/>
    /// Note that instances are actually created by calling <see cref="UnitContext.CreateInstances()"/>.
    /// </summary>
    public Type[] CreateInstanceTypes { get; private set; } = default!;

    /// <summary>
    /// Gets an array of command <see cref="Type"/>.
    /// </summary>
    public Type[] Commands => this.CommandDictionary[typeof(UnitBuilderContext.TopCommand)];

    /// <summary>
    /// Gets an array of subcommand <see cref="Type"/>.
    /// </summary>
    public Type[] Subcommands => this.CommandDictionary[typeof(UnitBuilderContext.SubCommand)];

    /// <summary>
    /// Gets a collection of command <see cref="Type"/> (keys) and subcommand <see cref="Type"/> (values).
    /// </summary>
    public Dictionary<Type, Type[]> CommandDictionary { get; private set; } = new();

    internal void AddRadio(UnitBase unit)
    {
        if (unit is IUnitPreparable configurable)
        {
            this.Radio.Open<UnitMessage.Prepare>(x => configurable.Prepare(x), unit);
        }

        if (unit is IUnitExecutable executable)
        {
            this.Radio.OpenAsync<UnitMessage.RunAsync>(x => executable.RunAsync(x), unit);
            this.Radio.OpenAsync<UnitMessage.TerminateAsync>(x => executable.TerminateAsync(x), unit);
        }

        if (unit is IUnitSerializable serializable)
        {
            this.Radio.OpenAsync<UnitMessage.LoadAsync>(x => serializable.LoadAsync(x), unit);
            this.Radio.OpenAsync<UnitMessage.SaveAsync>(x => serializable.SaveAsync(x), unit);
        }
    }
}
