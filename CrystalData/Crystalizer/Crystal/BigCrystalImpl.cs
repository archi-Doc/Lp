// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public class BigCrystalImpl<TData> : CrystalImpl<TData>, IBigCrystal<TData>, ICrystal
    where TData : BaseData, ITinyhandSerialize<TData>, ITinyhandReconstruct<TData>
{
    public BigCrystalImpl(Crystalizer crystalizer)
        : base(crystalizer)
    {
        this.BigCrystalConfiguration = crystalizer.GetBigCrystalConfiguration(typeof(TData));
        this.BigCrystalConfiguration.RegisterDatum(this.DatumRegistry);
        this.storageGroup = new(crystalizer);
        this.himoGoshujin = new(this);
        this.logger = crystalizer.UnitLogger.GetLogger<IBigCrystal<TData>>();

        this.storageFileConfiguration = this.BigCrystalConfiguration.DirectoryConfiguration.CombinePath(this.BigCrystalConfiguration.StorageFile);
        this.crystalFileConfiguration = this.BigCrystalConfiguration.DirectoryConfiguration.CombinePath(this.BigCrystalConfiguration.CrystalFile);

        this.InitializeRoot();
    }

    #region FieldAndProperty

    public BigCrystalConfiguration BigCrystalConfiguration { get; }

    public DatumRegistry DatumRegistry { get; } = new();

    public StorageGroup StorageGroup => this.storageGroup;

    public HimoGoshujinClass Himo => this.himoGoshujin;

    public long MemoryUsage => this.himoGoshujin.MemoryUsage;

    private StorageGroup storageGroup;
    private HimoGoshujinClass himoGoshujin;
    private ILogger logger;
    private PathConfiguration storageFileConfiguration;
    private IFiler? storageFiler;
    private PathConfiguration crystalFileConfiguration;
    private IFiler? crystalFiler;

    #endregion

    #region ICrystal

    public async Task<CrystalStartResult> PrepareAndLoad(CrystalStartParam? param)
    {
        param ??= CrystalStartParam.Default;
        using (this.semaphore.Lock())
        {
            this.storageFiler ??= this.Crystalizer.ResolveFiler(this.storageFileConfiguration);
            this.crystalFiler ??= this.Crystalizer.ResolveFiler(this.crystalFileConfiguration);

            if (param.FromScratch)
            {
                await this.StorageGroup.PrepareAndCheck(this.CrystalConfiguration.StorageConfiguration, param, null).ConfigureAwait(false);

                await this.DeleteAllAsync();
                this.InitializeRoot();

                return CrystalStartResult.Success;
            }

            // Load CrystalStorage
            var result = await this.LoadStorageGroup(param).ConfigureAwait(false);
            if (result != CrystalStartResult.Success)
            {
                return result;
            }

            var info = this.StorageGroup.GetInformation();
            foreach (var x in info)
            {
                this.logger.TryGet()?.Log(x);
            }

            // Load Crystal
            result = await this.LoadCrystal(param).ConfigureAwait(false);
            if (result != CrystalStartResult.Success)
            {
                return result;
            }

            return result;
        }
    }

    async Task<CrystalResult> ICrystal.Save(bool unload)
    {
        using (this.semaphore.Lock())
        {
            /*if (param.RemoveAll)
            {
                await this.DeleteAllAsync();

                // Stop storage
                this.StorageGroup.Clear();

                return;
            }*/

            // Save & Unload datum and metadata.
            this.obj?.Save(unload);

            // Stop storage
            await this.StorageGroup.SaveStorage().ConfigureAwait(false);

            // Save crystal
            await PathHelper.SaveData(this.obj, this.crystalFiler, 0).ConfigureAwait(false);

            // Save storage group
            if (this.storageFiler != null)
            {
                await this.StorageGroup.SaveGroup(this.storageFiler).ConfigureAwait(false);
            }

            // this.StorageGroup.Clear();

            this.logger.TryGet()?.Log($"Crystal stop - {this.himoGoshujin.MemoryUsage}");
        }

        return CrystalResult.Success;
    }

    public async Task Abort()
    {
        using (this.semaphore.Lock())
        {
            await this.StorageGroup.SaveStorage().ConfigureAwait(false);
            this.StorageGroup.Clear();
        }
    }

    /*void ICrystal.Delete()
    {
    }*/

    internal async Task DeleteAllAsync()
    {
        this.obj?.Delete();
        this.himoGoshujin.Clear();

        this.crystalFiler?.Delete();
        this.storageFiler?.Delete();
        await this.StorageGroup.DeleteAllAsync();

        this.InitializeRoot();
    }

    #endregion

    private void InitializeRoot()
    {
        this.obj = TinyhandSerializer.Reconstruct<TData>();
        this.obj.Initialize(this, null, true);
    }

    private async Task<CrystalStartResult> LoadStorageGroup(CrystalStartParam param)
    {// await this.semaphore.WaitAsync().ConfigureAwait(false)
        CrystalStartResult result;

        var (dataResult, _) = await PathHelper.LoadData(this.storageFiler).ConfigureAwait(false);
        if (dataResult.IsFailure)
        {
            if (await param.Query(CrystalStartResult.DirectoryNotFound).ConfigureAwait(false) == AbortOrComplete.Complete)
            {
                result = await this.StorageGroup.PrepareAndCheck(this.CrystalConfiguration.StorageConfiguration, param, null).ConfigureAwait(false);
                if (result == CrystalStartResult.Success || param.ForceStart)
                {
                    return CrystalStartResult.Success;
                }

                return result;
            }
            else
            {
                return CrystalStartResult.DirectoryNotFound;
            }
        }

        result = await this.StorageGroup.PrepareAndCheck(this.CrystalConfiguration.StorageConfiguration, param, dataResult.Data.Memory).ConfigureAwait(false);
        if (result == CrystalStartResult.Success || param.ForceStart)
        {
            return CrystalStartResult.Success;
        }

        return result;
    }

    private async Task<CrystalStartResult> LoadCrystal(CrystalStartParam param)
    {// await this.semaphore.WaitAsync().ConfigureAwait(false)
        var (dataResult, _) = await PathHelper.LoadData(this.crystalFiler).ConfigureAwait(false);
        if (dataResult.IsFailure)
        {
            if (await param.Query(CrystalStartResult.FileNotFound).ConfigureAwait(false) == AbortOrComplete.Complete)
            {
                return CrystalStartResult.Success;
            }
            else
            {
                return CrystalStartResult.FileNotFound;
            }
        }

        if (!this.DeserializeCrystal(dataResult.Data.Memory))
        {
            if (await param.Query(CrystalStartResult.FileError).ConfigureAwait(false) == AbortOrComplete.Complete)
            {
                return CrystalStartResult.Success;
            }
            else
            {
                return CrystalStartResult.FileError;
            }
        }

        return CrystalStartResult.Success;
    }

    private bool DeserializeCrystal(ReadOnlyMemory<byte> data)
    {
        if (!TinyhandSerializer.TryDeserialize<TData>(data.Span, out var tdata))
        {
            return false;
        }

        tdata.Initialize(this, null, true);
        this.obj = tdata;

        this.himoGoshujin.Clear();

        return true;
    }

    /*private async Task SaveCrystal(IFiler filer)
    {
        var byteArray = TinyhandSerializer.Serialize(this.obj);
        await HashHelper.GetFarmHashAndSaveAsync(byteArray, filer).ConfigureAwait(false);
    }*/
}
