﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using CrystalData.Filer;
using CrystalData.Storage;

namespace CrystalData;

public class Crystalizer
{
    public Crystalizer(UnitCore core, CrystalOptions options, ILogger logger, UnitLogger unitLogger, IStorageKey storageKey)
    {
        this.Core = core;
        this.Options = options;
        this.logger = logger;
        this.UnitLogger = unitLogger;
        this.StorageKey = storageKey;

        foreach (var x in this.Options.BigCrystalConfigurations)
        {
            // (ICrystalData) new CrystalDataImpl<TData>
            var bigCrystal = (IBigCrystal)Activator.CreateInstance(typeof(BigCrystalImpl<>).MakeGenericType(x.Key), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { this, }, null)!;

            this.typeToBigCrystal.TryAdd(x.Key, bigCrystal);
        }

        foreach (var x in this.Options.CrystalConfigurations)
        {
            ICrystal? crystal;
            if (!this.typeToBigCrystal.TryGetValue(x.Key, out var bigCrystal))
            {// Crystal
                // (ICrystal) new CrystalImpl<TData>
                crystal = (ICrystal)Activator.CreateInstance(typeof(CrystalImpl<>).MakeGenericType(x.Key), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { this, }, null)!;
            }
            else
            {// BigCrystal
                crystal = (ICrystal)bigCrystal;
            }

            crystal.Configure(x.Value);

            this.typeToCrystal.TryAdd(x.Key, crystal);
            this.crystals.TryAdd(crystal, 0);
        }
    }

    #region FieldAndProperty

    public UnitCore Core { get; }

    public CrystalOptions Options { get; }

    public IStorageKey StorageKey { get; }

    internal UnitLogger UnitLogger { get; }

    private ILogger logger;
    private ThreadsafeTypeKeyHashTable<ICrystal> typeToCrystal = new();
    private ConcurrentDictionary<ICrystal, int> crystals = new();
    private ThreadsafeTypeKeyHashTable<IBigCrystal> typeToBigCrystal = new();
    // private ConcurrentDictionary<IBigCrystal, int> bigCrystals = new();

    private object syncFiler = new();
    private LocalFiler? localFiler;
    private Dictionary<string, S3Filer> bucketToS3Filer = new();

    #endregion

    #region Resolvers

    public IFiler ResolveFiler(FilerConfiguration configuration)
    {
        lock (this.syncFiler)
        {
            if (configuration is EmptyFilerConfiguration emptyFilerConfiguration)
            {// Empty filer
                return new RawFilerToFiler(this, EmptyFiler.Default, configuration);
            }
            else if (configuration is LocalFilerConfiguration localFilerConfiguration)
            {// Local filer
                if (this.localFiler == null)
                {
                    this.localFiler ??= new LocalFiler();
                }

                return new RawFilerToFiler(this, this.localFiler, configuration);
            }
            else if (configuration is S3FilerConfiguration s3FilerConfiguration)
            {// S3 filer
                if (!this.bucketToS3Filer.TryGetValue(s3FilerConfiguration.Bucket, out var filer))
                {
                    filer = new S3Filer(s3FilerConfiguration.Bucket, string.Empty);
                    this.bucketToS3Filer.TryAdd(s3FilerConfiguration.Bucket, filer);
                }

                return new RawFilerToFiler(this, filer, configuration);
            }
            else
            {
                ThrowConfigurationNotRegistered(configuration.GetType());
                return default!;
            }
        }
    }

    public IStorage ResolveStorage(StorageConfiguration configuration)
    {
        lock (this.syncFiler)
        {
            if (configuration is EmptyStorageConfiguration emptyStorageonfiguration)
            {// Empty filer
            }

            return EmptyStorage.Default;
        }
    }

    #endregion

    #region Main

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

    public ICrystal[] GetArray()
    {
        return this.crystals.Keys.ToArray();
    }

    #endregion

    #region Misc

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
    internal IBigCrystal GetBigCrystal(Type type)
    {
        if (!this.typeToBigCrystal.TryGetValue(type, out var crystal))
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

    internal BigCrystalConfiguration GetCrystalConfiguration(Type type)
    {
        if (!this.Options.BigCrystalConfigurations.TryGetValue(type, out var crystalConfiguration))
        {
            ThrowTypeNotRegistered(type);
        }

        return crystalConfiguration!;
    }

    #endregion
}