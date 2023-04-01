// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using CrystalData.Filer;

namespace CrystalData;

public class Crystalizer
{
    public Crystalizer(CrystalizerOptions options)
    {
        this.options = options;

        foreach (var x in this.options.TypeToCrystalConfiguration)
        {
            // (ICrystal) new CrystalImpl<TData>
            var crystal = (ICrystal)Activator.CreateInstance(typeof(CrystalImpl<>).MakeGenericType(x.Key), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { this, }, null)!;
            crystal.Configure(x.Value);

            this.typeToCrystal.TryAdd(x.Key, crystal);
            this.crystals.TryAdd(crystal, 0);

            // IFiler<TData>, IStorage<TData>, IJournal<TData>
        }
    }

    #region FieldAndProperty

    private ThreadsafeTypeKeyHashTable<ICrystal> typeToCrystal = new();
    private ConcurrentDictionary<ICrystal, int> crystals = new();
    private ConcurrentDictionary<IFiler, int> filers = new();
    private ConcurrentDictionary<IStorage, int> storages = new();
    private ConcurrentDictionary<IJournal, int> journals = new();

    #endregion

    public async Task<CrystalStartResult> PrepareAndLoad(CrystalStartParam? param = null)
    {
        param ??= CrystalStartParam.Default;

        var crystals = this.crystals.Keys.ToArray();
        foreach (var x in crystals)
        {
            var result = await x.PrepareAndLoad(param).ConfigureAwait(false);
            if (result != CrystalStartResult.Success)
            {
                return result;
            }
        }

        return CrystalStartResult.Success;
    }

    public async Task SaveAndTerminate(CrystalStopParam? param = null)
    {
        param ??= CrystalStopParam.Default;

        var crystals = this.crystals.Keys.ToArray();
        foreach (var x in crystals)
        {
            await x.Save().ConfigureAwait(false);
        }
    }

    public ICrystal<T> Create<T>()
        where T : ITinyhandSerialize<T>, ITinyhandReconstruct<T>
    {
        if (!this.typeToCrystal.TryGetValue(typeof(T), out _))
        {
            ThrowTypeNotRegistered(typeof(T));
        }

        var crystal = new CrystalImpl<T>(this);
        this.crystals.TryAdd(crystal, 0);
        return crystal;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfNotRegistered<TData>()
    {
        if (!this.typeToCrystal.TryGetValue(typeof(TData), out _))
        {
            ThrowTypeNotRegistered(typeof(TData));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowTypeNotRegistered(Type type)
    {
        throw new InvalidOperationException($"The specified data type '{type.Name}' is not registered. Register the data type within ConfigureCrystal().");
    }

    internal bool DeleteInternal(ICrystal crystal)
    {
        return this.crystals.TryRemove(crystal, out _);
    }

    internal IFilerToCrystal GetFilerToCrystal(ICrystal crystal, FilerConfiguration filerConfiguration)
    {
    }

    internal IFiler ResolveFiler(Type type)
    {
        if (!this.typeToCrystal.TryGetValue(type, out var crystal))
        {
            ThrowTypeNotRegistered(type);
        }

        return crystal!;
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

        return ((ICrystal)crystal!).Object;
    }

    private CrystalizerOptions options;
}
