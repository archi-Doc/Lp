// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace CrystalData;

public class Crystalizer
{
    public Crystalizer(CrystalizerOptions options)
    {
        this.options = options;

        foreach (var x in this.options.TypeToCrystalConfiguration)
        {
            this.typeToCrystal.TryAdd(
                x.Key,
                Activator.CreateInstance(typeof(CrystalImpl<>).MakeGenericType(x.Key), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { this, }, null)!);
        }
    }

    private ThreadsafeTypeKeyHashTable<object> typeToCrystal = new();

    public ICrystal<T> Create<T>()
        where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>
    {
        if (!this.typeToCrystal.TryGetValue(typeof(T), out _))
        {
            ThrowTypeNotRegistered(typeof(T));
        }

        return new CrystalImpl<T>(this);
    }

    public ICrystal<T> Get<T>()
        where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>
    {
        if (!this.typeToCrystal.TryGetValue(typeof(T), out var crystal))
        {
            ThrowTypeNotRegistered(typeof(T));
        }

        return (ICrystal<T>)crystal!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowTypeNotRegistered(Type type)
    {
        throw new InvalidOperationException($"The specified data type '{type.Name}' is not registered. Register data type within ConfigureCrystal().");
    }

    internal object GetCrystal(Type type)
    {
        if (!this.typeToCrystal.TryGetValue(type, out var crystal))
        {
            ThrowTypeNotRegistered(type);
        }

        return crystal!;
    }

    internal object GetObject(Type type)
    {
        if (!this.typeToCrystal.TryGetValue(type, out var crystal))
        {
            ThrowTypeNotRegistered(type);
        }

        return ((ICrystalBase)crystal!).Object;
    }

    private CrystalizerOptions options;
}
