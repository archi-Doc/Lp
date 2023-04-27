// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Datum;

public interface ObjectDatum<TObject> : IDatum
    where TObject : ITinyhandSerialize<TObject>
{
    CrystalResult Set(TObject obj);

    Task<CrystalObjectResult<TObject>> Get();
}

public class ObjectDatumImpl<TObject> : HimoGoshujinClass.Himo, ObjectDatum<TObject>, IBaseDatum
    where TObject : ITinyhandSerialize<TObject>
{
    public ObjectDatumImpl(IDataInternal flakeInternal)
        : base(flakeInternal)
    {
    }

    public CrystalResult Set(TObject obj)
    {
        /*if (this.obj?.Equals(obj) == true)
        {// Identical
        }*/

        this.obj = obj;
        this.isSaved = false;
        return CrystalResult.Success;
    }

    public async Task<CrystalObjectResult<TObject>> Get()
    {
        if (this.obj != null)
        {
            this.UpdateHimo();
            return new(CrystalResult.Success, this.obj);
        }

        var result = await this.dataInternal.StorageToDatum<ObjectDatum<TObject>>().ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return new(result.Result);
        }

        var memory = result.Data.Memory;
        var obj = TinyhandSerializer.DeserializeObject<TObject>(memory.Span);
        if (obj == null)
        {
            return new(CrystalResult.DeserializeError);
        }

        this.obj = obj;
        this.isSaved = true;
        this.UpdateHimo(memory.Length);
        return new(CrystalResult.Success, this.obj);
    }

    private bool isSaved = true;
    private TObject? obj;

    void IBaseDatum.Save()
    {
        if (!this.isSaved)
        {// Not saved.
            if (this.obj != null &&
                SerializeHelper.TrySerialize(this.obj, out var owner))
            {
                this.dataInternal.DatumToStorage<ObjectDatum<TObject>>(owner.AsReadOnly());
                owner.Return();
            }

            this.isSaved = true;
        }
    }

    void IBaseDatum.Unload()
    {
        this.obj = default;
        this.RemoveHimo();
    }
}
