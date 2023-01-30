// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    internal class FlakeHimo : HimoGoshujinClass.Himo
    {
        public FlakeHimo(Zen<TIdentifier> zen, Flake flake)
            : base(zen.HimoGoshujin, flake)
        {
            this.HimoType = Type.FlakeHimo;
        }

        public void SetSpan(ReadOnlySpan<byte> data, bool clearSavedFlag)
        {// lock (Flake.syncObject)
            this.Update(this.SetSpanInternal(data), clearSavedFlag);
        }

        public void SetObject(object obj, bool clearSavedFlag)
        {// lock (Flake.syncObject)
            this.Update(this.SetObjectInternal(obj), clearSavedFlag);
        }

        public void SetMemoryOwner(ByteArrayPool.MemoryOwner dataToBeMoved, bool clearSavedFlag)
        {// lock (Flake.syncObject)
            this.Update(this.SetMemoryOwnerInternal(dataToBeMoved), clearSavedFlag);
        }

        public void SetMemoryOwner(ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved, bool clearSavedFlag)
        {// lock (Flake.syncObject)
            this.Update(this.SetMemoryOwnerInternal(dataToBeMoved), clearSavedFlag);
        }

        public bool TryGetSpan(out ReadOnlySpan<byte> data)
        {// lock (Flake.syncObject)
            return this.TryGetSpanInternal(out data);
        }

        public bool TryGetMemoryOwner(out ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        {// lock (Flake.syncObject)
            return this.TryGetMemoryOwnerInternal(out memoryOwner);
        }

        public bool TryGetObject([MaybeNullWhen(false)] out object? obj)
        {// lock (Flake.syncObject)
            return this.TryGetObjectInternal(out obj);
        }

        internal void UnloadInternal()
        {// lock (Flake.syncObject)
            var memoryDifference = this.Clear();
            this.Remove(memoryDifference);
        }

        internal void SaveInternal()
        {// lock (this.flake.syncObject)
            if (!this.isSaved)
            {// Not saved.
                if (this.TryGetMemoryOwnerInternal(out var memoryOwner))
                {
                    this.Flake.Zen.IO.Save(ref this.Flake.flakeFile, memoryOwner);
                    memoryOwner.Return();
                }

                this.isSaved = true;
            }
        }

        private (bool Changed, int MemoryDifference) SetSpanInternal(ReadOnlySpan<byte> span)
        {
            if (this.memoryOwnerAvailable &&
                span.SequenceEqual(this.memoryOwner.Memory.Span))
            {// Identical
                return (false, 0);
            }

            var memoryDifference = -this.memoryOwner.Memory.Length;
            this.memoryOwner = this.memoryOwner.Return();

            memoryDifference += span.Length;
            this.@object = null;
            var owner = FlakeFragmentPool.Rent(span.Length);
            this.memoryOwner = owner.ToReadOnlyMemoryOwner(0, span.Length);
            this.memoryOwnerAvailable = true;
            span.CopyTo(owner.ByteArray.AsSpan());
            return (true, memoryDifference);
        }

        private (bool Changed, int MemoryDifference) SetMemoryOwnerInternal(ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved)
        {
            if (this.memoryOwnerAvailable &&
                dataToBeMoved.Memory.Span.SequenceEqual(this.memoryOwner.Memory.Span))
            {// Identical
                return (false, 0);
            }

            var memoryDifference = -this.memoryOwner.Memory.Length;
            this.memoryOwner = this.memoryOwner.Return();

            memoryDifference += dataToBeMoved.Memory.Length;
            this.@object = null;
            this.memoryOwner = dataToBeMoved;
            this.memoryOwnerAvailable = true;
            return (true, memoryDifference);
        }

        private (bool Changed, int MemoryDifference) SetMemoryOwnerInternal(ByteArrayPool.MemoryOwner dataToBeMoved)
            => this.SetMemoryOwnerInternal(dataToBeMoved.AsReadOnly());

        private (bool Changed, int MemoryDifference) SetObjectInternal(object? obj)
        {
            /* Skip (may be an updated object of the same instance)
            if (obj == this.Object)
            {// Identical
                return 0;
            }*/

            var memoryDifference = -this.memoryOwner.Memory.Length;
            this.memoryOwner = this.memoryOwner.Return();
            this.memoryOwnerAvailable = false;

            this.@object = obj;
            return (true, memoryDifference);
        }

        private bool TryGetSpanInternal(out ReadOnlySpan<byte> data)
        {
            if (this.memoryOwnerAvailable)
            {
                data = this.memoryOwner.Memory.Span;
                return true;
            }
            else if (this.@object != null && this.Flake.Zen.ObjectToMemoryOwner(this.@object, out var m))
            {
                this.memoryOwner = m.AsReadOnly();
                this.memoryOwnerAvailable = true;
                data = this.memoryOwner.Memory.Span;
                return true;
            }
            else
            {
                data = default;
                return false;
            }
        }

        private bool TryGetMemoryOwnerInternal(out ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        {
            if (this.memoryOwnerAvailable)
            {
                memoryOwner = this.memoryOwner.IncrementAndShare();
                return true;
            }
            else if (this.@object != null && this.Flake.Zen.ObjectToMemoryOwner(this.@object, out var m))
            {
                this.memoryOwner = m.AsReadOnly();
                this.memoryOwnerAvailable = true;
                memoryOwner = this.memoryOwner.IncrementAndShare();
                return true;
            }
            else
            {
                memoryOwner = default;
                return false;
            }
        }

        private bool TryGetObjectInternal([MaybeNullWhen(false)] out object? obj)
        {
            if (this.@object != null)
            {
                obj = this.@object;
                return true;
            }
            else if (this.memoryOwnerAvailable)
            {
                obj = this.Flake.Zen.MemoryOwnerToObject(this.memoryOwner);
                return obj != null;
            }
            else
            {
                obj = default;
                return false;
            }
        }

        private int Clear()
        {
            this.@object = null;
            var memoryDifference = -this.memoryOwner.Memory.Length;
            this.memoryOwner = this.memoryOwner.Return();
            this.memoryOwnerAvailable = false;
            return memoryDifference;
        }

        private object? @object;
        private bool memoryOwnerAvailable;
        private ByteArrayPool.ReadOnlyMemoryOwner memoryOwner;
    }
}
