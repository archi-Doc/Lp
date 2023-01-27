// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

#pragma warning disable SA1401 // Fields should be private

public partial class Zen<TIdentifier>
{
    internal partial class FragmentHimo : Himo
    {
        public enum Result
        {
            Success,
            NotFound,
            NotLoaded,
        }

        public FragmentHimo(Flake flake, HimoGoshujinClass goshujin)
            : base(flake, goshujin)
        {
        }

        public ZenResult SetSpan(TIdentifier fragmentId, ReadOnlySpan<byte> data)
        {// lock (Flake.syncObject)
            if (this.fragments == null)
            {
                this.fragments = this.PrepareFragments();
            }

            FragmentData? fragmentData;
            if (this.fragments.IdChain.Count >= this.Flake.Zen.Options.MaxFragmentCount)
            {
                return ZenResult.OverNumberLimit;
            }
            else if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragmentData))
            {
                fragmentData = new(this.Flake.Zen, fragmentId);
                this.fragments.Add(fragmentData);
            }

            this.UpdateQueue(HimoOperation.Set, fragmentData.SetSpan(data));
            return ZenResult.Success;
        }

        public ZenResult SetObject(TIdentifier fragmentId, object obj)
        {// lock (Flake.syncObject)
            if (this.fragments == null)
            {
                this.fragments = this.PrepareFragments();
            }

            FragmentData? fragmentData;
            if (this.fragments.IdChain.Count >= this.Flake.Zen.Options.MaxFragmentCount)
            {
                return ZenResult.OverNumberLimit;
            }
            else if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragmentData))
            {
                fragmentData = new(this.Flake.Zen, fragmentId);
                this.fragments.Add(fragmentData);
            }

            this.UpdateQueue(HimoOperation.Set, fragmentData.SetObject(obj));
            return ZenResult.Success;
        }

        public ZenResult SetMemoryOwner(TIdentifier fragmentId, ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved)
        {// lock (Flake.syncObject)
            if (this.fragments == null)
            {
                this.fragments = this.PrepareFragments();
            }

            FragmentData? fragmentData;
            if (this.fragments.IdChain.Count >= this.Flake.Zen.Options.MaxFragmentCount)
            {
                return ZenResult.OverNumberLimit;
            }
            else if (!this.fragments.IdChain.TryGetValue(fragmentId, out fragmentData))
            {
                fragmentData = new(this.Flake.Zen, fragmentId);
                this.fragments.Add(fragmentData);
            }

            this.UpdateQueue(HimoOperation.Set, fragmentData.SetMemoryOwner(dataToBeMoved));
            return ZenResult.Success;
        }

        public bool TryGetSpan(TIdentifier fragmentId, out ReadOnlySpan<byte> data)
        {// lock (Flake.syncObject)
            if (this.fragments == null)
            {
                this.fragments = this.PrepareFragments();
            }

            if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragment))
            {// Fount
                return fragment.TryGetSpan(out data);
            }
            else
            {
                data = default;
                return false;
            }
        }

        public Result TryGetMemoryOwner(TIdentifier fragmentId, out ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        {// lock (Flake.syncObject)
            if (this.fragments == null)
            {
                memoryOwner = default;
                return Result.NotLoaded;
            }

            if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragmentData))
            {// Fount
                if (fragmentData.TryGetMemoryOwner(out memoryOwner))
                {
                    return Result.Success;
                }
                else
                {
                    return Result.NotFound;
                }
            }

            memoryOwner = default;
            return Result.NotFound;
        }

        public Result TryGetObject(TIdentifier fragmentId, [MaybeNullWhen(false)] out object? obj)
        {// lock (Flake.syncObject)
            if (this.fragments == null)
            {
                obj = default;
                return Result.NotLoaded;
            }

            if (this.fragments.IdChain.TryGetValue(fragmentId, out var fragmentData))
            {// Fount
                if (fragmentData.TryGetObject(out obj))
                {
                    return Result.Success;
                }
                else
                {
                    return Result.NotFound;
                }
            }

            obj = default;
            return Result.NotFound;
        }

        public void Unload()
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

            this.RemoveQueue(memoryDifference);
        }

        public bool Remove(TIdentifier fragmentId)
        {// lock (Flake.syncObject)
            if (this.fragments == null)
            {
                this.fragments = this.PrepareFragments();
            }

            if (!this.fragments.IdChain.TryGetValue(fragmentId, out var fragmentData))
            {// Not found
                return false;
            }

            this.fragments.Remove(fragmentData);
            this.UpdateQueue(HimoOperation.Remove, (true, fragmentData.Clear()));
            return true;
        }

        internal override void Save(bool unload)
        {// lock (Flake.syncObject) -> lock (this.FlakeObjectGoshujin.Goshujin)
            if (this.fragments != null)
            {
                var writer = default(Tinyhand.IO.TinyhandWriter);
                var options = TinyhandSerializerOptions.Standard;
                foreach (var x in this.fragments.IdChain)
                {
                    if (x.TryGetSpan(out var span))
                    {
                        TinyhandSerializer.SerializeObject(ref writer, x.TIdentifier, options); // x.TIdentifier.Serialize(ref writer, options);
                        writer.Write(span);
                    }
                }

                var memoryOwner = new ByteArrayPool.ReadOnlyMemoryOwner(writer.FlushAndGetArray());
                this.Flake.Zen.IO.Save(ref this.Flake.fragmentFile, memoryOwner);
            }

            if (unload)
            {// Unload
                this.Unload();
            }
        }

        internal bool Load(ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        {
            if (this.fragments != null)
            {// Already loaded
                return true;
            }

            this.fragments = new();
            var reader = new Tinyhand.IO.TinyhandReader(memoryOwner.Memory.Span);
            var options = TinyhandSerializerOptions.Standard;
            try
            {
                while (!reader.End)
                {
                    var identifier = default(TIdentifier);
                    TinyhandSerializer.DeserializeObject(ref reader, ref identifier, options); // identifier.Deserialize(ref reader, options);
                    var byteArray = reader.ReadBytesToArray();

                    var fragment = new FragmentData(this.Flake.Zen, identifier!);
                    fragment.SetSpan(byteArray);
                    this.fragments.Add(fragment);
                }
            }
            finally
            {
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
                    if (this.Load(result.Data))
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
                    if (this.Load(result.Data))
                    {
                        return this.fragments!;
                    }
                }
            }

            return new();
        }

        private FragmentData.GoshujinClass? fragments; // by Yamamoto.
    }
}
