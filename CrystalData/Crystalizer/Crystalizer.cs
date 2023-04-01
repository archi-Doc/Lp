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
        }
    }

    #region FieldAndProperty

    private ThreadsafeTypeKeyHashTable<ICrystal> typeToCrystal = new();
    private ConcurrentDictionary<ICrystal, int> crystals = new();

    private object syncFiler = new();
    private LocalFiler? localFiler;
    private Dictionary<string, S3Filer> bucketToS3Filer = new();

    #endregion

    #region Resolvers

    public IFiler ResolveFiler(FilerConfiguration filerConfiguration)
    {
        lock (this.syncFiler)
        {
            if (filerConfiguration is EmptyFilerConfiguration emptyFilerConfiguration)
            {// Empty filer
                return new RawFilerToFiler(this, EmptyFiler.Default, string.Empty);
            }
            else if (filerConfiguration is LocalFilerConfiguration localFilerConfiguration)
            {// Local filer
                if (this.localFiler == null)
                {
                    this.localFiler ??= new LocalFiler(string.Empty);
                }

                return new RawFilerToFiler(this, this.localFiler, localFilerConfiguration.File);
            }
            else if (filerConfiguration is S3FilerConfiguration s3FilerConfiguration)
            {// S3 filer
                if (!this.bucketToS3Filer.TryGetValue(s3FilerConfiguration.Bucket, out var filer))
                {
                    filer = new S3Filer(s3FilerConfiguration.Bucket, string.Empty);
                    this.bucketToS3Filer.TryAdd(s3FilerConfiguration.Bucket, filer);
                }

                return new RawFilerToFiler(this, filer, s3FilerConfiguration.File);
            }
            else
            {
                ThrowConfigurationNotRegistered(filerConfiguration.GetType());
                return default!;
            }
        }
    }

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

    public ICrystal<TData> Create<TData>()
        where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
    {
        if (!this.typeToCrystal.TryGetValue(typeof(TData), out _))
        {
            ThrowTypeNotRegistered(typeof(TData));
        }

        var crystal = new CrystalImpl<TData>(this);
        this.crystals.TryAdd(crystal, 0);
        return crystal;
    }

    public ICrystal<TData> Get<TData>()
        where TData : ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
    {
        if (!this.typeToCrystal.TryGetValue(typeof(TData), out var crystal))
        {
            ThrowTypeNotRegistered(typeof(TData));
        }

        return (ICrystal<TData>)crystal!;
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowConfigurationNotRegistered(Type type)
    {
        throw new InvalidOperationException($"The specified configuration type '{type.Name}' is not registered.");
    }

    internal bool DeleteInternal(ICrystal crystal)
    {
        return this.crystals.TryRemove(crystal, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ICrystal GetCrystal(Type type)
    {
        if (!this.typeToCrystal.TryGetValue(type, out var crystal))
        {
            ThrowTypeNotRegistered(type);
        }

        return crystal!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal object GetObject(Type type)
    {
        if (!this.typeToCrystal.TryGetValue(type, out var crystal))
        {
            ThrowTypeNotRegistered(type);
        }

        return crystal!.Object;
    }

    private CrystalizerOptions options;
}
