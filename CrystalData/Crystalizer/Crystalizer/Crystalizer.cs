// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using CrystalData.Check;
using CrystalData.Filer;
using CrystalData.Journal;
using CrystalData.Storage;

#pragma warning disable SA1204

namespace CrystalData;

public class Crystalizer
{
    public const string CheckFile = "Crystal.check";

    public Crystalizer(CrystalizerConfiguration configuration, CrystalizerOptions options, ILogger logger, UnitLogger unitLogger, IStorageKey storageKey)
    {
        this.configuration = configuration;
        this.EnableLogger = options.EnableLogger;
        this.RootDirectory = options.RootPath;
        this.DefaultTimeout = options.DefaultTimeout;
        this.MemorySizeLimit = options.MemorySizeLimit;
        this.MaxParentInMemory = options.MaxParentInMemory;
        if (string.IsNullOrEmpty(this.RootDirectory))
        {
            this.RootDirectory = Directory.GetCurrentDirectory();
        }

        this.logger = logger;
        this.UnitLogger = unitLogger;
        this.CrystalCheck = new(this.UnitLogger.GetLogger<CrystalCheck>());
        this.CrystalCheck.Load(Path.Combine(this.RootDirectory, CheckFile));
        this.Himo = new(this);
        this.StorageKey = storageKey;

        foreach (var x in this.configuration.CrystalConfigurations)
        {
            ICrystal? crystal;
            if (x.Value is BigCrystalConfiguration bigCrystalConfiguration)
            {// new BigCrystalImpl<TData>
                var bigCrystal = (IBigCrystal)Activator.CreateInstance(typeof(BigCrystalObject<>).MakeGenericType(x.Key), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { this, }, null)!;
                crystal = bigCrystal;
                bigCrystal.Configure(bigCrystalConfiguration);
            }
            else
            {// new CrystalImpl<TData>
                crystal = (ICrystal)Activator.CreateInstance(typeof(CrystalObject<>).MakeGenericType(x.Key), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { this, }, null)!;
                crystal.Configure(x.Value);
            }

            this.typeToCrystal.TryAdd(x.Key, crystal);
            this.crystals.TryAdd(crystal, 0);
        }
    }

    #region FieldAndProperty

    public bool EnableLogger { get; }

    public string RootDirectory { get; }

    public TimeSpan DefaultTimeout { get; }

    public long MemorySizeLimit { get; }

    public int MaxParentInMemory { get; }

    public IJournal? Journal { get; private set; }

    public IStorageKey StorageKey { get; }

    public HimoGoshujinClass Himo { get; }

    internal UnitLogger UnitLogger { get; }

    internal CrystalCheck CrystalCheck { get; }

    private CrystalizerConfiguration configuration;
    private ILogger logger;
    private ThreadsafeTypeKeyHashTable<ICrystal> typeToCrystal = new(); // Type to ICrystal
    private ConcurrentDictionary<ICrystal, int> crystals = new(); // All crystals
    private ConcurrentDictionary<uint, ICrystal> planeToCrystal = new(); // Plane to crystal

    private object syncFiler = new();
    private IRawFiler? localFiler;
    private Dictionary<string, IRawFiler> bucketToS3Filer = new();

    #endregion

    #region Resolvers

    public IFiler ResolveFiler(PathConfiguration configuration)
    {// new RawFilerToFiler(this, this.ResolveRawFiler(configuration), configuration);
        string path = configuration.Path;

        /*if (this.AddExtension)
        {
            try
            {
                if (string.IsNullOrEmpty(Path.GetExtension(configuration.Path)))
                {
                    path += "." + Extension;
                }
            }
            catch
            {
            }
        }*/

        return new RawFilerToFiler(this, this.ResolveRawFiler(configuration), path);
    }

    public IRawFiler ResolveRawFiler(PathConfiguration configuration)
    {
        lock (this.syncFiler)
        {
            if (configuration is EmptyFileConfiguration ||
                configuration is EmptyDirectoryConfiguration)
            {// Empty file or directory
                return EmptyFiler.Default;
            }
            else if (configuration is LocalFileConfiguration ||
                configuration is LocalDirectoryConfiguration)
            {// Local file or directory
                if (this.localFiler == null)
                {
                    this.localFiler ??= new LocalFiler();
                }

                return this.localFiler;
            }
            else if (configuration is S3FileConfiguration s3FilerConfiguration)
            {// S3 file
                return ResolveS3Filer(s3FilerConfiguration.Bucket);
            }
            else if (configuration is S3DirectoryConfiguration s3DirectoryConfiguration)
            {// S3 directory
                return ResolveS3Filer(s3DirectoryConfiguration.Bucket);
            }
            else
            {
                ThrowConfigurationNotRegistered(configuration.GetType());
                return default!;
            }
        }

        IRawFiler ResolveS3Filer(string bucket)
        {
            if (!this.bucketToS3Filer.TryGetValue(bucket, out var filer))
            {
                filer = new S3Filer(bucket);
                this.bucketToS3Filer.TryAdd(bucket, filer);
            }

            return filer;
        }
    }

    public IStorage ResolveStorage(StorageConfiguration configuration)
    {
        lock (this.syncFiler)
        {
            IStorage storage;
            if (configuration is EmptyStorageConfiguration emptyStorageConfiguration)
            {// Empty storage
                storage = EmptyStorage.Default;
            }
            else if (configuration is SimpleStorageConfiguration simpleStorageConfiguration)
            {
                storage = new SimpleStorage(this);
            }
            else
            {
                ThrowConfigurationNotRegistered(configuration.GetType());
                return default!;
            }

            storage.SetTimeout(this.DefaultTimeout);
            return storage;
        }
    }

    #endregion

    #region Main

    public void ResetConfigurations()
    {
        foreach (var x in this.configuration.CrystalConfigurations)
        {
            if (this.typeToCrystal.TryGetValue(x.Key, out var crystal))
            {
                if (x.Value is BigCrystalConfiguration bigCrystalConfiguration &&
                    crystal is IBigCrystal bigCrystal)
                {
                    bigCrystal.Configure(bigCrystalConfiguration);
                }
                else
                {
                    crystal.Configure(x.Value);
                }
            }
        }
    }

    public async Task<CrystalResult> SaveConfigurations(FileConfiguration configuration)
    {
        var data = TinyhandSerializer.ReconstructObject<CrystalizerConfigurationData>();
        foreach (var x in this.configuration.CrystalConfigurations)
        {
            if (this.typeToCrystal.TryGetValue(x.Key, out var crystal) &&
                x.Key.FullName is { } name)
            {
                if (crystal.CrystalConfiguration is BigCrystalConfiguration bigCrystalConfiguration)
                {
                    data.BigCrystalConfigurations[name] = bigCrystalConfiguration;
                }
                else
                {
                    data.CrystalConfigurations[name] = crystal.CrystalConfiguration;
                }
            }
        }

        var filer = this.ResolveFiler(configuration);
        var result = await filer.PrepareAndCheck(PrepareParam.ContinueAll<Crystalizer>(this), configuration).ConfigureAwait(false);
        if (result.IsFailure())
        {
            return result;
        }

        var bytes = TinyhandSerializer.SerializeToUtf8(data);
        result = await filer.WriteAsync(0, new(bytes)).ConfigureAwait(false);

        return result;
    }

    public async Task<CrystalResult> LoadConfigurations(FileConfiguration configuration)
    {
        var filer = this.ResolveFiler(configuration);
        var result = await filer.PrepareAndCheck(PrepareParam.ContinueAll<Crystalizer>(this), configuration).ConfigureAwait(false);
        if (result.IsFailure())
        {
            return result;
        }

        var readResult = await filer.ReadAsync(0, -1).ConfigureAwait(false);
        if (readResult.IsFailure)
        {
            return readResult.Result;
        }

        try
        {
            var data = TinyhandSerializer.DeserializeFromUtf8<CrystalizerConfigurationData>(readResult.Data.Memory);
            if (data == null)
            {
                return CrystalResult.DeserializeError;
            }

            var nameToCrystal = new Dictionary<string, ICrystal>();
            foreach (var x in this.typeToCrystal.ToArray())
            {
                if (x.Key.FullName is { } name)
                {
                    nameToCrystal[name] = x.Value;
                }
            }

            foreach (var x in data.CrystalConfigurations)
            {
                if (nameToCrystal.TryGetValue(x.Key, out var crystal))
                {
                    crystal.Configure(x.Value);
                }
            }

            foreach (var x in data.BigCrystalConfigurations)
            {
                if (nameToCrystal.TryGetValue(x.Key, out var crystal) &&
                    crystal is IBigCrystal bigCrystal)
                {
                    bigCrystal.Configure(x.Value);
                }
            }

            return CrystalResult.Success;
        }
        catch
        {
            return CrystalResult.DeserializeError;
        }
        finally
        {
            readResult.Return();
        }
    }

    public async Task<CrystalResult> PrepareAndLoadAll(CrystalPrepare? param = null)
    {
        param ??= CrystalPrepare.ContinueAll;

        // Journal
        var result = await this.PrepareJournal(param).ConfigureAwait(false);
        if (result.IsFailure())
        {
            return result;
        }

        // Crystals
        var crystals = this.crystals.Keys.ToArray();
        foreach (var x in crystals)
        {
            result = await x.PrepareAndLoad(param).ConfigureAwait(false);
            if (result.IsFailure())
            {
                return result;
            }
        }

        return CrystalResult.Success;
    }

    public async Task SaveAll(bool unload = false)
    {
        this.CrystalCheck.Save();

        var crystals = this.crystals.Keys.ToArray();
        foreach (var x in crystals)
        {
            await x.Save(unload).ConfigureAwait(false);
        }
    }

    public async Task SaveAllAndTerminate()
    {
        await this.SaveAll(true).ConfigureAwait(false);

        // Terminate journal
        if (this.Journal is { } journal)
        {
            await journal.TerminateAsync().ConfigureAwait(false);
        }

        // Terminate filers/journal
        var tasks = new List<Task>();
        lock (this.syncFiler)
        {
            if (this.localFiler is not null)
            {
                tasks.Add(this.localFiler.TerminateAsync());
                this.localFiler = null;
            }

            foreach (var x in this.bucketToS3Filer.Values)
            {
                tasks.Add(x.TerminateAsync());
            }

            this.bucketToS3Filer.Clear();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        this.logger.TryGet()?.Log($"Crystal stop - {this.Himo.MemoryUsage}");
    }

    public async Task<CrystalResult[]> DeleteAll()
    {
        var tasks = this.crystals.Keys.Select(x => x.Delete()).ToArray();
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }

    public ICrystal<TData> CreateCrystal<TData>()
        where TData : class, IJournalObject, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
    {
        var crystal = new CrystalObject<TData>(this);
        this.crystals.TryAdd(crystal, 0);
        return crystal;
    }

    public ICrystal<TData> CreateBigCrystal<TData>()
        where TData : BaseData, IJournalObject, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
    {
        var crystal = new BigCrystalObject<TData>(this);
        this.crystals.TryAdd(crystal, 0);
        return crystal;
    }

    public ICrystal<TData> GetCrystal<TData>()
        where TData : class, IJournalObject, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
    {
        if (!this.typeToCrystal.TryGetValue(typeof(TData), out var c) ||
            c is not ICrystal<TData> crystal)
        {
            ThrowTypeNotRegistered(typeof(TData));
            return default!;
        }

        return crystal;
    }

    public IBigCrystal<TData> GetBigCrystal<TData>()
        where TData : BaseData, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
    {
        if (!this.typeToCrystal.TryGetValue(typeof(TData), out var c) ||
            c is not IBigCrystal<TData> crystal)
        {
            ThrowTypeNotRegistered(typeof(TData));
            return default!;
        }

        return crystal;
    }

    #endregion

    #region Waypoint/Plane

    internal void UpdatePlane(ICrystal crystal, ref Waypoint waypoint, ulong hash)
    {
        if (waypoint.CurrentPlane != 0)
        {// Remove the current plane
            this.planeToCrystal.TryRemove(waypoint.CurrentPlane, out _);
        }

        // Next plane
        var nextPlane = waypoint.NextPlane;
        if (nextPlane == 0)
        {
            while (true)
            {
                nextPlane = RandomVault.Pseudo.NextUInt32();
                if (nextPlane != 0 && this.planeToCrystal.TryAdd(nextPlane, crystal))
                {// Success
                    break;
                }
            }
        }

        // New plane
        uint newPlane;
        while (true)
        {
            newPlane = RandomVault.Pseudo.NextUInt32();
            if (newPlane != 0 && this.planeToCrystal.TryAdd(newPlane, crystal))
            {// Success
                break;
            }
        }

        // Current/Next -> Next/New

        // Add journal
        ulong journalPosition;
        if (this.Journal != null)
        {
            this.Journal.GetWriter(JournalRecordType.Waypoint, nextPlane, out var writer);
            writer.Write(newPlane);
            writer.Write(hash);
            journalPosition = this.Journal.Add(writer);
        }
        else
        {
            journalPosition = waypoint.JournalPosition + 1;
        }

        waypoint = new(journalPosition, nextPlane, newPlane, hash);
    }

    internal void RemovePlane(Waypoint waypoint)
    {
        if (waypoint.CurrentPlane != 0)
        {
            this.planeToCrystal.TryRemove(waypoint.CurrentPlane, out _);
        }

        if (waypoint.NextPlane != 0)
        {
            this.planeToCrystal.TryRemove(waypoint.NextPlane, out _);
        }
    }

    internal void SetPlane(ICrystal crystal, ref Waypoint waypoint)
    {
        if (waypoint.CurrentPlane != 0)
        {
            this.planeToCrystal[waypoint.CurrentPlane] = crystal;
        }

        if (waypoint.NextPlane != 0)
        {
            this.planeToCrystal[waypoint.NextPlane] = crystal;
        }
    }

    #endregion

    #region Misc

    internal static string GetRootedFile(Crystalizer? crystalizer, string file)
        => crystalizer == null ? file : PathHelper.GetRootedFile(crystalizer.RootDirectory, file);

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

    /*internal CrystalConfiguration GetCrystalConfiguration(Type type)
    {
        if (!this.configuration.CrystalConfigurations.TryGetValue(type, out var configuration))
        {
            ThrowTypeNotRegistered(type);
        }

        return configuration!;
    }

    internal BigCrystalConfiguration GetBigCrystalConfiguration(Type type)
    {
        if (!this.configuration.CrystalConfigurations.TryGetValue(type, out var configuration))
        {
            ThrowTypeNotRegistered(type);
        }

        if (configuration is not BigCrystalConfiguration bigCrystalConfiguration)
        {
            ThrowTypeNotRegistered(type);
            return default!;
        }

        return bigCrystalConfiguration;
    }*/

    private async Task<CrystalResult> PrepareJournal(CrystalPrepare prepare)
    {
        if (this.Journal == null)
        {// New journal
            var configuration = this.configuration.JournalConfiguration;
            if (configuration is EmptyJournalConfiguration)
            {
                return CrystalResult.Success;
            }
            else if (configuration is SimpleJournalConfiguration simpleJournalConfiguration)
            {
                var simpleJournal = new SimpleJournal(this, simpleJournalConfiguration);
                this.Journal = simpleJournal;
            }
            else
            {
                return CrystalResult.NotFound;
            }
        }

        return await this.Journal.Prepare(prepare.ToParam<Crystalizer>(this)).ConfigureAwait(false);
    }

    #endregion
}
