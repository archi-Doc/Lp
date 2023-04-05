// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using CrystalData.Filer;
using CrystalData.Storage;

namespace CrystalData;

public class Crystalizer
{
    public Crystalizer(CrystalizerConfiguration configuration, CrystalizerOptions options, ILogger logger, UnitLogger unitLogger, IStorageKey storageKey)
    {
        this.configuration = configuration;
        this.EnableLogger = options.EnableLogger;
        this.RootDirectory = options.RootPath;
        if (string.IsNullOrEmpty(this.RootDirectory))
        {
            this.RootDirectory = Directory.GetCurrentDirectory();
        }

        this.logger = logger;
        this.UnitLogger = unitLogger;
        this.StorageKey = storageKey;

        foreach (var x in this.configuration.BigCrystalConfigurations)
        {
            // (IBigCrystal) new CrystalDataImpl<TData>
            var bigCrystal = (IBigCrystal)Activator.CreateInstance(typeof(BigCrystalImpl<>).MakeGenericType(x.Key), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { this, }, null)!;

            this.typeToBigCrystal.TryAdd(x.Key, bigCrystal);
        }

        foreach (var x in this.configuration.CrystalConfigurations)
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

    public bool EnableLogger { get; }

    public string RootDirectory { get; }

    public IStorageKey StorageKey { get; }

    internal UnitLogger UnitLogger { get; }

    private CrystalizerConfiguration configuration;
    private ILogger logger;
    private ThreadsafeTypeKeyHashTable<ICrystal> typeToCrystal = new();
    private ConcurrentDictionary<ICrystal, int> crystals = new();
    private ThreadsafeTypeKeyHashTable<IBigCrystal> typeToBigCrystal = new();
    // private ConcurrentDictionary<IBigCrystal, int> bigCrystals = new();

    private object syncObject = new();
    private LocalFiler? localFiler;
    private Dictionary<string, S3Filer> bucketToS3Filer = new();

    #endregion

    #region Resolvers

    public IFiler ResolveFiler(FilerConfiguration configuration)
        => new RawFilerToFiler(this, this.ResolveRawFiler(configuration), configuration);

    public IRawFiler ResolveRawFiler(FilerConfiguration configuration)
    {
        lock (this.syncObject)
        {
            if (configuration is EmptyFilerConfiguration emptyFilerConfiguration)
            {// Empty filer
                return EmptyFiler.Default;
            }
            else if (configuration is LocalFilerConfiguration localFilerConfiguration)
            {// Local filer
                if (this.localFiler == null)
                {
                    this.localFiler ??= new LocalFiler();
                }

                return this.localFiler;
            }
            else if (configuration is S3FilerConfiguration s3FilerConfiguration)
            {// S3 filer
                if (!this.bucketToS3Filer.TryGetValue(s3FilerConfiguration.Bucket, out var filer))
                {
                    filer = new S3Filer(s3FilerConfiguration.Bucket, string.Empty);
                    this.bucketToS3Filer.TryAdd(s3FilerConfiguration.Bucket, filer);
                }

                return filer;
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
        lock (this.syncObject)
        {
            if (configuration is EmptyStorageConfiguration emptyStorageConfiguration)
            {// Empty storage
                return EmptyStorage.Default;
            }
            else if (configuration is SimpleStorageConfiguration simpleStorageConfiguration)
            {
                return new SimpleStorage();
            }
            else
            {
                ThrowConfigurationNotRegistered(configuration.GetType());
                return default!;
            }
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

    internal CrystalConfiguration GetCrystalConfiguration(Type type)
    {
        if (!this.configuration.CrystalConfigurations.TryGetValue(type, out var configuration))
        {
            ThrowTypeNotRegistered(type);
        }

        return configuration!;
    }

    internal BigCrystalConfiguration GetBigCrystalConfiguration(Type type)
    {
        if (!this.configuration.BigCrystalConfigurations.TryGetValue(type, out var configuration))
        {
            ThrowTypeNotRegistered(type);
        }

        configuration!.BigCrystalOptions.SetRootPath(this.RootDirectory);
        return configuration!;
    }

    #endregion
}
