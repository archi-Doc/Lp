// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    [ValueLinkObject]
    internal partial class FragmentObject : MemoryOwnerAndObject
    {
        public FragmentObject(TIdentifier identifier)
            : base()
        {
            this.TIdentifier = identifier;
        }

        [Link(Primary = true, NoValue = true, Name = "Id", Type = ChainType.Unordered)]
        [Link(Name = "OrderedId", Type = ChainType.Ordered)]
        public TIdentifier TIdentifier { get; private set; }
    }

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

    }

    internal class FragmentDataImpl : HimoGoshujinClass.Himo, FragmentData, IBaseData
    {
        public FragmentDataImpl(IFlakeInternal flakeInternal)
            : base(flakeInternal)
        {
        }

        public override int Id => FragmentData.Id;

        public ZenResult Set(TIdentifier fragmentId, ReadOnlySpan<byte> span)
        {
            if (span.Length > this.flakeInternal.Options.MaxFragmentSize)
            {
                return ZenResult.OverSizeLimit;
            }

            return this.fragmentHimo.SetSpan(fragmentId, span, true);
        }

        public ZenResult SetObject<T>(TIdentifier fragmentId, T obj)
            where T : ITinyhandSerialize<T>
        {
            if (!FlakeFragmentService.TrySerialize(obj, out var memoryOwner))
            {
                return ZenResult.SerializeError;
            }
            else if (memoryOwner.Memory.Length > this.flakeInternal.Options.MaxFragmentSize)
            {
                return ZenResult.OverSizeLimit;
            }

            return this.fragmentHimo.SetMemoryOwner(fragmentId, memoryOwner.AsReadOnly(), obj, true);
        }

        public async Task<ZenMemoryResult> Get(TIdentifier fragmentId)
        {
            ulong file = 0;

            if (this.fragmentHimo != null)
            {// Memory
                var fragmentResult = this.fragmentHimo.TryGetMemoryOwner(fragmentId, out var memoryOwner);
                if (fragmentResult == FragmentHimo.Result.Success)
                {
                    return new(ZenResult.Success, memoryOwner.Memory); // Skip MemoryOwner.Return()
                }
                else if (fragmentResult == FragmentHimo.Result.NotFound)
                {
                    return new(ZenResult.NoData);
                }
            }
            else
            {// Load
                file = this.fragmentFile;
            }

            if (ZenHelper.IsValidFile(file))
            {
                var result = await this.Zen.IO.Load(file);
                if (!result.IsSuccess)
                {
                    return new(result.Result);
                }

                using (this.semaphore.Lock())
                {
                    if (this.IsRemoved)
                    {
                        return new(ZenResult.Removed);
                    }

                    this.fragmentHimo ??= new(this);
                    this.fragmentHimo.LoadInternal(result.Data);

                    var fragmentResult = this.fragmentHimo.TryGetMemoryOwner(fragmentId, out var memoryOwner);
                    if (fragmentResult == FragmentHimo.Result.Success)
                    {
                        return new(ZenResult.Success, memoryOwner.IncrementAndShare().Memory); // Skip MemoryOwner.Return()
                    }
                }
            }

            return new(ZenResult.NoData);
        }

        public async Task<ZenObjectResult<T>> GetObject<T>(TIdentifier fragmentId)
            where T : ITinyhandSerialize<T>
        {
            ulong file = 0;
            using (this.semaphore.Lock())
            {
                if (this.IsRemoved)
                {
                    return new(ZenResult.Removed);
                }

                if (this.fragmentHimo != null)
                {// Memory
                    var fragmentResult = this.fragmentHimo.TryGetObject(fragmentId, out T? obj);
                    return new(fragmentResult, obj);
                }
                else
                {// Load
                    file = this.fragmentFile;
                }
            }

            if (ZenHelper.IsValidFile(file))
            {
                var result = await this.Zen.IO.Load(file);
                if (!result.IsSuccess)
                {
                    return new(result.Result);
                }

                using (this.semaphore.Lock())
                {
                    if (this.IsRemoved)
                    {
                        return new(ZenResult.Removed);
                    }

                    this.fragmentHimo ??= new(this);
                    this.fragmentHimo.LoadInternal(result.Data);

                    var fragmentResult = this.fragmentHimo.TryGetObject(fragmentId, out T? obj);
                    return new(fragmentResult, obj);
                }
            }

            return new(ZenResult.NoData);
        }

        public bool RemoveFragment(TIdentifier fragmentId)
        {
            return this.RemoveInternal(fragmentId);
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
                    this.flakeInternal.SaveInternal<FragmentData>(memoryOwner);
                }

                this.isSaved = true;
            }
        }

        public void Unload()
        {
        }

        private bool RemoveInternal(TIdentifier fragmentId)
        {// using (Flake.semaphore)
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

            var result = this.flakeInternal.LoadInternal<FragmentData>().Result;
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

            var result = await this.flakeInternal.LoadInternal<FragmentData>().ConfigureAwait(false);
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

        public ZenResult SetSpan(TIdentifier fragmentId, ReadOnlySpan<byte> data, bool clearSavedFlag)
        {// using (Flake.semaphore)
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

        public ZenResult SetMemoryOwner(TIdentifier fragmentId, ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved, object? obj, bool clearSavedFlag)
        {// using (Flake.semaphore)
            if (this.fragments == null)
            {
                this.fragments = this.PrepareFragments();
            }

            FragmentObject? fragmentObject;
            if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragmentObject))
            {// New
                if (this.fragments.IdChain.Count >= this.Flake.Zen.Options.MaxFragmentCount)
                {
                    return ZenResult.OverNumberLimit;
                }

                fragmentObject = new(fragmentId);
                fragmentObject.Goshujin = this.fragments;
            }

            this.Update(fragmentObject.SetMemoryOwnerInternal(dataToBeMoved, obj), clearSavedFlag);
            return ZenResult.Success;
        }

        public Result TryGetMemoryOwner(TIdentifier fragmentId, out ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        {// using (Flake.semaphore)
            if (this.fragments == null)
            {
                memoryOwner = default;
                return Result.NotLoaded;
            }

            if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragmentData))
            {// Found
                memoryOwner = fragmentData.MemoryOwner.IncrementAndShare();
                return Result.Success;
            }

            memoryOwner = default;
            return Result.NotFound;
        }

        public ZenResult TryGetObject<T>(TIdentifier fragmentId, out T? obj)
            where T : ITinyhandSerialize<T>
        {// using (Flake.semaphore)
            if (this.fragments == null)
            {
                obj = default;
                return ZenResult.NoData;
            }

            if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragmentData))
            {// Found
                var result = fragmentData.TryGetObjectInternal(out obj);
                this.Update();
                return result;
            }

            obj = default;
            return ZenResult.NoData;
        }
    }
}
