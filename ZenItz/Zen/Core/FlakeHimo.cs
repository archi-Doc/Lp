// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    internal class FlakeHimo : Himo
    {
        public FlakeHimo(Flake flake, HimoGoshujinClass goshujin)
            : base(flake, goshujin)
        {
            this.fragment = new(flake.Zen);
        }

        public void SetSpan(ReadOnlySpan<byte> data)
        {// lock (Flake.syncObject)
            this.UpdateQueue(HimoOperation.Set, this.fragment.SetSpan(data));
        }

        public void SetObject(object obj)
        {// lock (Flake.syncObject)
            this.UpdateQueue(HimoOperation.Set, this.fragment.SetObject(obj));
        }

        public void SetMemoryOwner(ByteArrayPool.MemoryOwner dataToBeMoved)
        {// lock (Flake.syncObject)
            this.UpdateQueue(HimoOperation.Set, this.fragment.SetMemoryOwner(dataToBeMoved));
        }

        public void SetMemoryOwner(ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved)
        {// lock (Flake.syncObject)
            this.UpdateQueue(HimoOperation.Set, this.fragment.SetMemoryOwner(dataToBeMoved));
        }

        public bool TryGetSpan(out ReadOnlySpan<byte> data)
        {// lock (Flake.syncObject)
            return this.fragment.TryGetSpan(out data);
        }

        public bool TryGetMemoryOwner(out ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        {// lock (Flake.syncObject)
            return this.fragment.TryGetMemoryOwner(out memoryOwner);
        }

        public bool TryGetObject([MaybeNullWhen(false)] out object? obj)
        {// lock (Flake.syncObject)
            return this.fragment.TryGetObject(out obj);
        }

        public void Unload()
        {// lock (Flake.syncObject)
            this.RemoveQueue(this.fragment.Clear());
        }

        internal override void Save(bool unload)
        {// lock (this.FlakeObjectGoshujin.Goshujin)
            if (!this.IsSaved)
            {// Not saved.
                if (this.fragment.TryGetMemoryOwner(out var memoryOwner))
                {
                    this.Flake.Zen.IO.Save(ref this.Flake.flakeFile, memoryOwner);
                    memoryOwner.Return();
                }

                this.IsSaved = true;
            }

            if (unload)
            {// Unload
                this.Unload();
            }
        }

        private FlakeData fragment;
    }
}
