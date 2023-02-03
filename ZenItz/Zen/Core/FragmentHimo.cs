// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    internal partial class FragmentHimo : HimoGoshujinClass.Himo
    {
        public enum Result
        {
            Success,
            NotFound,
            NotLoaded,
        }

        public FragmentHimo(Flake flake)
            : base(flake)
        {
            this.HimoType = Type.FragmentHimo;
        }

        public ZenResult SetSpan(TIdentifier fragmentId, ReadOnlySpan<byte> data, bool clearSavedFlag)
        {// lock (Flake.syncObject)
            if (this.fragments == null)
            {
                this.fragments = this.PrepareFragments();
            }

            FragmentData? fragmentData;
            if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragmentData))
            {// New
                if (this.fragments.Count >= this.Flake.Zen.Options.MaxFragmentCount)
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
        {// lock (Flake.syncObject)
            if (this.fragments == null)
            {
                this.fragments = this.PrepareFragments();
            }

            FragmentData? fragmentData;
            if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragmentData))
            {// New
                if (this.fragments.IdChain.Count >= this.Flake.Zen.Options.MaxFragmentCount)
                {
                    return ZenResult.OverNumberLimit;
                }

                fragmentData = new(fragmentId);
                fragmentData.Goshujin = this.fragments;
            }

            this.Update(fragmentData.SetMemoryOwnerInternal(dataToBeMoved, obj), clearSavedFlag);
            return ZenResult.Success;
        }

        /*public ZenResult SetObject(TIdentifier fragmentId, ITinyhandSerialize obj, bool clearSavedFlag)
        {// lock (Flake.syncObject)
            if (this.fragments == null)
            {
                this.fragments = this.PrepareFragments();
            }

            FragmentData? fragmentData;
            if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragmentData))
            {// New
                if (this.fragments.IdChain.Count >= this.Flake.Zen.Options.MaxFragmentCount)
                {
                    return ZenResult.OverNumberLimit;
                }

                fragmentData = new(fragmentId);
                this.fragments.Add(fragmentData);
            }

            this.Update(fragmentData.SetObjectInternal(obj), clearSavedFlag);
            return ZenResult.Success;
        }*/

        /*public bool TryGetSpan(TIdentifier fragmentId, out ReadOnlySpan<byte> data)
        {// lock (Flake.syncObject)
            if (this.fragments == null)
            {
                this.fragments = this.PrepareFragments();
            }

            if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragment))
            {// Found
                var result = fragment.TryGetSpanInternal(out data);
                this.Update(result.MemoryDifference);
                return result.Result;
            }
            else
            {
                data = default;
                return false;
            }
        }*/

        public Result TryGetMemoryOwner(TIdentifier fragmentId, out ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        {// lock (Flake.syncObject)
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
        {// lock (Flake.syncObject)
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

        internal void UnloadInternal()
        {// lock (Flake.syncObject)
            int memoryDifference = 0;
            if (this.fragments != null)
            {
                foreach (var x in this.fragments.IdChain)
                {
                    memoryDifference += x.Clear();
                }

                this.fragments.Clear();
                this.fragments = null;
            }

            this.Remove(memoryDifference);
        }

        internal bool RemoveInternal(TIdentifier fragmentId)
        {// lock (Flake.syncObject)
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

        internal void SaveInternal()
        {// lock (Flake.syncObject)
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
                    this.Flake.Zen.IO.Save(ref this.Flake.fragmentFile, memoryOwner);
                }

                this.isSaved = true;
            }
        }

        internal bool LoadInternal(ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        {
            if (this.fragments != null)
            {// Already loaded
                return true;
            }

            this.fragments = new();
            var max = this.Flake.Zen.Options.MaxFragmentCount;
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
                        var fragment = new FragmentData(identifier!);
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

        private FragmentData.GoshujinClass PrepareFragments()
        {// lock (Flake.syncObject)
            if (this.fragments != null)
            {
                return this.fragments;
            }
            else if (ZenFile.IsValidFile(this.Flake.fragmentFile))
            {
                var result = this.Flake.Zen.IO.Load(this.Flake.fragmentFile).Result;
                if (result.IsSuccess)
                {
                    if (this.LoadInternal(result.Data))
                    {
                        return this.fragments!;
                    }
                }
            }

            return new();
        }

        private async Task<FragmentData.GoshujinClass> PrepareFragmentsAsync()
        {// lock (Flake.syncObject)
            if (this.fragments != null)
            {
                return this.fragments;
            }
            else if (ZenFile.IsValidFile(this.Flake.fragmentFile))
            {
                var result = await this.Flake.Zen.IO.Load(this.Flake.fragmentFile).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    if (this.LoadInternal(result.Data))
                    {
                        return this.fragments!;
                    }
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
        private FragmentData.GoshujinClass? fragments; // by Yamamoto.
    }
}
