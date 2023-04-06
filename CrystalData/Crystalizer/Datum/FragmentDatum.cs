// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace CrystalData.Datum;

public interface FragmentDatum<TIdentifier> : IDatum
    where TIdentifier : IEquatable<TIdentifier>, IComparable<TIdentifier>, ITinyhandSerialize<TIdentifier>
{
    CrystalResult Set(TIdentifier fragmentId, ReadOnlySpan<byte> span);

    CrystalResult SetObject<T>(TIdentifier fragmentId, T obj)
        where T : ITinyhandSerialize<T>;

    Task<CrystalMemoryResult> Get(TIdentifier fragmentId);

    Task<CrystalObjectResult<T>> GetObject<T>(TIdentifier fragmentId)
        where T : ITinyhandSerialize<T>;

    bool Remove(TIdentifier fragmentId);
}

public class FragmentDatumImpl<TIdentifier> : HimoGoshujinClass.Himo, FragmentDatum<TIdentifier>, IBaseDatum
    where TIdentifier : IEquatable<TIdentifier>, IComparable<TIdentifier>, ITinyhandSerialize<TIdentifier>
{
    public FragmentDatumImpl(IDataInternal flakeInternal)
        : base(flakeInternal)
    {
    }

    CrystalResult FragmentDatum<TIdentifier>.Set(TIdentifier fragmentId, ReadOnlySpan<byte> span)
    {
        if (span.Length > this.dataInternal.BigCrystalConfiguration.MaxFragmentSize)
        {
            return CrystalResult.OverSizeLimit;
        }

        return this.SetSpan(fragmentId, span, true);
    }

    CrystalResult FragmentDatum<TIdentifier>.SetObject<T>(TIdentifier fragmentId, T obj)
    {
        if (!SerializeHelper.TrySerialize(obj, out var memoryOwner))
        {
            return CrystalResult.SerializeError;
        }
        else if (memoryOwner.Memory.Length > this.dataInternal.BigCrystalConfiguration.MaxFragmentSize)
        {
            return CrystalResult.OverSizeLimit;
        }

        return this.SetMemoryOwner(fragmentId, memoryOwner.AsReadOnly(), obj, true);
    }

    async Task<CrystalMemoryResult> FragmentDatum<TIdentifier>.Get(TIdentifier fragmentId)
    {
        if (this.fragments == null)
        {
            this.fragments = await this.PrepareFragmentsAsync().ConfigureAwait(false);
        }

        if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragmentData))
        {// Found
            this.UpdateHimo();
            return new(CrystalResult.Success, fragmentData.MemoryOwner.IncrementAndShare().Memory);
        }

        return new(CrystalResult.NoDatum);
    }

    async Task<CrystalObjectResult<T>> FragmentDatum<TIdentifier>.GetObject<T>(TIdentifier fragmentId)
    {
        if (this.fragments == null)
        {
            this.fragments = await this.PrepareFragmentsAsync().ConfigureAwait(false);
        }

        if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragmentData))
        {// Found
            var result = fragmentData.TryGetObjectInternal<T>(out var obj);
            this.UpdateHimo();
            return new(result, obj);
        }

        return new(CrystalResult.NoDatum);
    }

    bool FragmentDatum<TIdentifier>.Remove(TIdentifier fragmentId)
    {
        if (this.fragments == null)
        {
            this.fragments = this.PrepareFragments();
        }

        if (!this.fragments.IdChain.TryGetValue(fragmentId, out var fragmentData))
        {// Not found
            return false;
        }

        fragmentData.Goshujin = null;
        fragmentData.Clear();
        this.RemoveHimo();
        return true;
    }

    public void Save()
    {
        if (!this.isSaved)
        {
            if (this.fragments != null)
            {
                var writer = default(Tinyhand.IO.TinyhandWriter);
                var options = TinyhandSerializerOptions.Standard;
                foreach (var x in this.fragments)
                {
                    TinyhandSerializer.SerializeObject(ref writer, x.Identifier, options); // x.TIdentifier.Serialize(ref writer, options);
                    writer.Write(x.Span);
                }

                var memoryOwner = new ByteArrayPool.ReadOnlyMemoryOwner(writer.FlushAndGetArray());
                this.dataInternal.DatumToStorage<FragmentDatum<TIdentifier>>(memoryOwner);
            }

            this.isSaved = true;
        }
    }

    public void Unload()
    {
        if (this.fragments is not null)
        {
            foreach (var x in this.fragments)
            {
                x.Clear();
            }
        }

        this.RemoveHimo();
    }

    private bool LoadInternal(ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
    {
        if (this.fragments != null)
        {// Already loaded
            return true;
        }

        this.fragments = new();
        var max = this.dataInternal.BigCrystalConfiguration.MaxFragmentCount;
        var reader = new Tinyhand.IO.TinyhandReader(memoryOwner.Memory.Span);
        var options = TinyhandSerializerOptions.Standard;
        var memoryDifference = 0;
        try
        {
            while (!reader.End)
            {
                var identifier = default(TIdentifier);
                TinyhandSerializer.DeserializeObject(ref reader, ref identifier, options); // identifier.Deserialize(ref reader, options);
                var byteArray = reader.ReadBytesToArray();

                if (this.fragments.Count < max)
                {
                    var fragment = new FragmentObject<TIdentifier>(identifier!);
                    var result = fragment.SetSpanInternal(byteArray);
                    memoryDifference += result.NewSize;
                    this.fragments.Add(fragment);
                }
            }
        }
        finally
        {
            this.AddCurrentSize(memoryDifference);
        }

        return true;
    }

    private FragmentObject<TIdentifier>.GoshujinClass PrepareFragments()
    {// using (Flake.semaphore)
        if (this.fragments != null)
        {
            return this.fragments;
        }

        var result = this.dataInternal.StorageToDatum<FragmentDatum<TIdentifier>>().Result;
        if (result.IsSuccess)
        {
            if (this.LoadInternal(result.Data))
            {
                return this.fragments!;
            }
        }

        return new();
    }

    private async Task<FragmentObject<TIdentifier>.GoshujinClass> PrepareFragmentsAsync()
    {// using (Flake.semaphore)
        if (this.fragments != null)
        {
            return this.fragments;
        }

        var result = await this.dataInternal.StorageToDatum<FragmentDatum<TIdentifier>>().ConfigureAwait(false);
        if (result.IsSuccess)
        {
            if (this.LoadInternal(result.Data))
            {
                return this.fragments!;
            }
        }

        return new();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update((bool Changed, int MemoryDifference) result, bool clearSavedFlag)
    {
        if (clearSavedFlag && result.Changed)
        {
            this.isSaved = false;
        }

        this.UpdateHimo(result.MemoryDifference);
    }

    private bool isSaved = true;
    private FragmentObject<TIdentifier>.GoshujinClass? fragments; // by Yamamoto.

    private CrystalResult SetSpan(TIdentifier fragmentId, ReadOnlySpan<byte> data, bool clearSavedFlag)
    {
        if (this.fragments == null)
        {
            this.fragments = this.PrepareFragments();
        }

        FragmentObject<TIdentifier>? fragmentData;
        if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragmentData))
        {// New
            if (this.fragments.Count >= this.dataInternal.BigCrystalConfiguration.MaxFragmentCount)
            {
                return CrystalResult.OverNumberLimit;
            }

            fragmentData = new(fragmentId);
            fragmentData.Goshujin = this.fragments;
        }

        this.Update(fragmentData.SetSpanInternal(data), clearSavedFlag);
        return CrystalResult.Success;
    }

    private CrystalResult SetMemoryOwner(TIdentifier fragmentId, ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved, object? obj, bool clearSavedFlag)
    {// using (Flake.semaphore)
        if (this.fragments == null)
        {
            this.fragments = this.PrepareFragments();
        }

        FragmentObject<TIdentifier>? fragmentObject;
        if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragmentObject))
        {// New
            if (this.fragments.IdChain.Count >= this.dataInternal.BigCrystalConfiguration.MaxFragmentCount)
            {
                return CrystalResult.OverNumberLimit;
            }

            fragmentObject = new(fragmentId);
            fragmentObject.Goshujin = this.fragments;
        }

        this.Update(fragmentObject.SetMemoryOwnerInternal(dataToBeMoved, obj), clearSavedFlag);
        return CrystalResult.Success;
    }
}
