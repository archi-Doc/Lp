// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    public interface FragmentData : IData
    {
        const int Id = 2;

        static int IData.StaticId => Id;

        ZenResult Set(TIdentifier fragmentId, ReadOnlySpan<byte> span);

        ZenResult SetObject<T>(TIdentifier fragmentId, T obj)
            where T : ITinyhandSerialize<T>;

        Task<ZenMemoryResult> Get(TIdentifier fragmentId);

        Task<ZenObjectResult<T>> GetObject<T>(TIdentifier fragmentId)
            where T : ITinyhandSerialize<T>;

        bool Remove(TIdentifier fragmentId);
    }

    internal class FragmentDataImpl : HimoGoshujinClass.Himo, FragmentData, IBaseData
    {
        public FragmentDataImpl(IFlakeInternal flakeInternal)
            : base(flakeInternal)
        {
        }

        public override int Id => FragmentData.Id;

        ZenResult FragmentData.Set(TIdentifier fragmentId, ReadOnlySpan<byte> span)
        {
            if (span.Length > this.flakeInternal.Options.MaxFragmentSize)
            {
                return ZenResult.OverSizeLimit;
            }

            return this.SetSpan(fragmentId, span, true);
        }

        ZenResult FragmentData.SetObject<T>(TIdentifier fragmentId, T obj)
        {
            if (!FlakeFragmentService.TrySerialize(obj, out var memoryOwner))
            {
                return ZenResult.SerializeError;
            }
            else if (memoryOwner.Memory.Length > this.flakeInternal.Options.MaxFragmentSize)
            {
                return ZenResult.OverSizeLimit;
            }

            return this.SetMemoryOwner(fragmentId, memoryOwner.AsReadOnly(), obj, true);
        }

        async Task<ZenMemoryResult> FragmentData.Get(TIdentifier fragmentId)
        {
            if (this.fragments == null)
            {
                this.fragments = await this.PrepareFragmentsAsync().ConfigureAwait(false);
            }

            if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragmentData))
            {// Found
                this.Update();
                return new(ZenResult.Success, fragmentData.MemoryOwner.IncrementAndShare().Memory);
            }

            return new(ZenResult.NoData);
        }

        async Task<ZenObjectResult<T>> FragmentData.GetObject<T>(TIdentifier fragmentId)
        {
            if (this.fragments == null)
            {
                this.fragments = await this.PrepareFragmentsAsync().ConfigureAwait(false);
            }

            if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragmentData))
            {// Found
                var result = fragmentData.TryGetObjectInternal<T>(out var obj);
                this.Update();
                return new(result, obj);
            }

            return new(ZenResult.NoData);
        }

        bool FragmentData.Remove(TIdentifier fragmentId)
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
            this.Remove(fragmentData.Clear());
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
                    var memoryDifference = 0;
                    foreach (var x in this.fragments)
                    {
                        TinyhandSerializer.SerializeObject(ref writer, x.TIdentifier, options); // x.TIdentifier.Serialize(ref writer, options);
                        writer.Write(x.Span);
                    }

                    this.Change(memoryDifference);
                    var memoryOwner = new ByteArrayPool.ReadOnlyMemoryOwner(writer.FlushAndGetArray());
                    this.flakeInternal.DataToStorage<FragmentData>(memoryOwner);
                }

                this.isSaved = true;
            }
        }

        public void Unload()
        {
        }

        private bool LoadInternal(ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        {
            if (this.fragments != null)
            {// Already loaded
                return true;
            }

            this.fragments = new();
            var max = this.flakeInternal.Options.MaxFragmentCount;
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
                        var fragment = new FragmentObject(identifier!);
                        var result = fragment.SetSpanInternal(byteArray);
                        memoryDifference += result.MemoryDifference;
                        this.fragments.Add(fragment);
                    }
                }
            }
            finally
            {
                this.Change(memoryDifference);
            }

            return true;
        }

        private FragmentObject.GoshujinClass PrepareFragments()
        {// using (Flake.semaphore)
            if (this.fragments != null)
            {
                return this.fragments;
            }

            var result = this.flakeInternal.StorageToData<FragmentData>().Result;
            if (result.IsSuccess)
            {
                if (this.LoadInternal(result.Data))
                {
                    return this.fragments!;
                }
            }

            return new();
        }

        private async Task<FragmentObject.GoshujinClass> PrepareFragmentsAsync()
        {// using (Flake.semaphore)
            if (this.fragments != null)
            {
                return this.fragments;
            }

            var result = await this.flakeInternal.StorageToData<FragmentData>().ConfigureAwait(false);
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

            this.Update(result.MemoryDifference);
        }

        private bool isSaved = true;
        private FragmentObject.GoshujinClass? fragments; // by Yamamoto.

        private ZenResult SetSpan(TIdentifier fragmentId, ReadOnlySpan<byte> data, bool clearSavedFlag)
        {
            if (this.fragments == null)
            {
                this.fragments = this.PrepareFragments();
            }

            FragmentObject? fragmentData;
            if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragmentData))
            {// New
                if (this.fragments.Count >= this.flakeInternal.Options.MaxFragmentCount)
                {
                    return ZenResult.OverNumberLimit;
                }

                fragmentData = new(fragmentId);
                fragmentData.Goshujin = this.fragments;
            }

            this.Update(fragmentData.SetSpanInternal(data), clearSavedFlag);
            return ZenResult.Success;
        }

        private ZenResult SetMemoryOwner(TIdentifier fragmentId, ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved, object? obj, bool clearSavedFlag)
        {// using (Flake.semaphore)
            if (this.fragments == null)
            {
                this.fragments = this.PrepareFragments();
            }

            FragmentObject? fragmentObject;
            if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragmentObject))
            {// New
                if (this.fragments.IdChain.Count >= this.flakeInternal.Options.MaxFragmentCount)
                {
                    return ZenResult.OverNumberLimit;
                }

                fragmentObject = new(fragmentId);
                fragmentObject.Goshujin = this.fragments;
            }

            this.Update(fragmentObject.SetMemoryOwnerInternal(dataToBeMoved, obj), clearSavedFlag);
            return ZenResult.Success;
        }
    }
}
