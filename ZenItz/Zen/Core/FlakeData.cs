// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    internal partial class FlakeData
    {
        public FlakeData(Zen<TIdentifier> zen)
        {
            this.Zen = zen;
        }

        protected (bool Changed, int MemoryDifference) SetSpanInternal(ReadOnlySpan<byte> span)
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

        protected (bool Changed, int MemoryDifference) SetMemoryOwnerInternal(ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved)
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

        protected (bool Changed, int MemoryDifference) SetMemoryOwnerInternal(ByteArrayPool.MemoryOwner dataToBeMoved)
            => this.SetMemoryOwnerInternal(dataToBeMoved.AsReadOnly());

        protected (bool Changed, int MemoryDifference) SetObjectInternal(object? obj)
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

        protected bool TryGetSpanInternal(out ReadOnlySpan<byte> data)
        {
            if (this.memoryOwnerAvailable)
            {
                data = this.memoryOwner.Memory.Span;
                return true;
            }
            else if (this.@object != null && this.Zen.ObjectToMemoryOwner(this.@object, out var m))
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

        protected bool TryGetMemoryOwnerInternal(out ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        {
            if (this.memoryOwnerAvailable)
            {
                memoryOwner = this.memoryOwner.IncrementAndShare();
                return true;
            }
            else if (this.@object != null && this.Zen.ObjectToMemoryOwner(this.@object, out var m))
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

        protected bool TryGetObjectInternal([MaybeNullWhen(false)] out object? obj)
        {
            if (this.@object != null)
            {
                obj = this.@object;
                return true;
            }
            else if (this.memoryOwnerAvailable)
            {
                obj = this.Zen.MemoryOwnerToObject(this.memoryOwner);
                return obj != null;
            }
            else
            {
                obj = default;
                return false;
            }
        }

        protected int Clear()
        {
            this.@object = null;
            var memoryDifference = -this.memoryOwner.Memory.Length;
            this.memoryOwner = this.memoryOwner.Return();
            this.memoryOwnerAvailable = false;
            return memoryDifference;
        }

        public Zen<TIdentifier> Zen { get; }

        private object? @object;
        private bool memoryOwnerAvailable;
        private ByteArrayPool.ReadOnlyMemoryOwner memoryOwner;
    }
}
