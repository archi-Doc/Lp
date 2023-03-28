// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace CrystalData;

public class Crystalizer
{
    public Crystalizer(CrystalizerOptions options)
    {
        this.options = options;
    }

    private ThreadsafeTypeKeyHashTable<object> typeToCrystal = new();

    public ICrystal<T> Create<T>()
        where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>
    {
        if (!this.typeToCrystal.TryGetValue(typeof(T), out _))
        {
            ThrowTypeNotRegistered();
        }

        return new CrystalImpl<T>(this);
    }

    public ICrystal<T> Get<T>()
        where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>
    {
        if (!this.typeToCrystal.TryGetValue(typeof(T), out var crystal))
        {
            ThrowTypeNotRegistered();
        }

        return (ICrystal<T>)crystal!;
    }

    internal object GetInternal(Type type)
    {
        if (!this.typeToCrystal.TryGetValue(type, out var crystal))
        {
            ThrowTypeNotRegistered();
        }

        return crystal!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowTypeNotRegistered()
    {
        throw new InvalidOperationException("The specified data type is not registered. Call ConfigureCrystal() to register.");
    }

    private CrystalizerOptions options;
}
