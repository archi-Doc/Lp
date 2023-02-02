// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    internal class FlakeHimo : HimoGoshujinClass.Himo
    {
        public FlakeHimo(Flake flake)
            : base(flake)
        {
            this.HimoType = Type.FlakeHimo;
        }

        public void SetSpan(ReadOnlySpan<byte> data, bool clearSavedFlag)
        {// lock (Flake.syncObject)
            this.Update(this.flakeData.SetSpanInternal(data), clearSavedFlag);
        }

        public void SetMemoryOwner(ByteArrayPool.MemoryOwner dataToBeMoved, bool clearSavedFlag)
        {// lock (Flake.syncObject)
            this.Update(this.flakeData.SetMemoryOwnerInternal(dataToBeMoved), clearSavedFlag);
        }

        public void SetMemoryOwner(ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved, bool clearSavedFlag)
        {// lock (Flake.syncObject)
            this.Update(this.flakeData.SetMemoryOwnerInternal(dataToBeMoved), clearSavedFlag);
        }

        public void SetObject(ITinyhandSerialize obj, bool clearSavedFlag)
        {// lock (Flake.syncObject)
            this.Update(this.flakeData.SetObjectInternal(obj), clearSavedFlag);
        }

        public bool TryGetSpan(out ReadOnlySpan<byte> data)
        {// lock (Flake.syncObject)
            var result = this.flakeData.TryGetSpanInternal(out data);
            this.Update(result.MemoryDifference);
            return result.Result;
        }

        public bool TryGetMemoryOwner(out ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        {// lock (Flake.syncObject)
            var result = this.flakeData.TryGetMemoryOwnerInternal(out memoryOwner);
            this.Update(result.MemoryDifference);
            return result.Result;
        }

        public ZenResult TryGetObject<T>(out T? obj)
            where T : ITinyhandSerialize<T>
        {// lock (Flake.syncObject)
            var result = this.flakeData.TryGetObjectInternal(out obj);
            this.Update(result.MemoryDifference);
            return result.Result;
        }

        internal void UnloadInternal()
        {// lock (Flake.syncObject)
            var memoryDifference = this.flakeData.Clear();
            this.Remove(memoryDifference);
        }

        internal void SaveInternal()
        {// lock (this.flake.syncObject)
            if (!this.isSaved)
            {// Not saved.
                var result = this.flakeData.TryGetMemoryOwnerInternal(out var memoryOwner);
                this.Change(result.MemoryDifference);
                if (result.Result)
                {
                    this.Flake.Zen.IO.Save(ref this.Flake.flakeFile, memoryOwner);
                    memoryOwner.Return();
                }

                this.isSaved = true;
            }
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
        private FlakeData flakeData = new();
    }
}
