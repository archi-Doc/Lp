// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    internal partial class FlakeData
    {
        public FlakeData()
        {
        }

        internal (bool Changed, int MemoryDifference) SetSpanInternal(ReadOnlySpan<byte> span)
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

        internal (bool Changed, int MemoryDifference) SetMemoryOwnerInternal(ByteArrayPool.ReadOnlyMemoryOwner dataToBeMoved)
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

        internal (bool Changed, int MemoryDifference) SetMemoryOwnerInternal(ByteArrayPool.MemoryOwner dataToBeMoved)
            => this.SetMemoryOwnerInternal(dataToBeMoved.AsReadOnly());

        internal (bool Changed, int MemoryDifference) SetObjectInternal(ITinyhandSerialize obj)
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

        internal (bool Result, int MemoryDifference) TryGetSpanInternal(out ReadOnlySpan<byte> data)
        {
            if (this.memoryOwnerAvailable)
            {
                data = this.memoryOwner.Memory.Span;
                return (true, 0);
            }
            else if (this.@object != null)
            {
                if (this.@object.TrySerialize(out var m))
                {
                    this.memoryOwner = m.AsReadOnly();
                    this.memoryOwnerAvailable = true;
                    data = this.memoryOwner.Memory.Span;
                    return (true, this.memoryOwner.Memory.Length);
                }
            }

            data = default;
            return (false, 0);
        }

        internal (bool Result, int MemoryDifference) TryGetMemoryOwnerInternal(out ByteArrayPool.ReadOnlyMemoryOwner memoryOwner)
        {
            if (this.memoryOwnerAvailable)
            {
                memoryOwner = this.memoryOwner.IncrementAndShare();
                return (true, 0);
            }
            else if (this.@object != null)
            {
                if (this.@object.TrySerialize(out var m))
                {
                    this.memoryOwner = m.AsReadOnly();
                    this.memoryOwnerAvailable = true;
                    memoryOwner = this.memoryOwner.IncrementAndShare();
                    return (true, this.memoryOwner.Memory.Length);
                }
            }

            memoryOwner = default;
            return (false, 0);
        }

        internal (ZenResult Result, int MemoryDifference) TryGetObjectInternal<T>(out T? obj)
            where T : ITinyhandSerialize<T>
        {
            if (this.@object is T t)
            {
                obj = t;
                return (ZenResult.Success, 0);
            }
            else if (this.memoryOwnerAvailable)
            {
                try
                {
                    obj = TinyhandSerializer.DeserializeObject<T>(this.memoryOwner.Memory.Span);
                    if (obj != null)
                    {
                        return (ZenResult.Success, 0);
                    }
                }
                catch
                {
                }

                obj = default;
                return (ZenResult.DeserializeError, 0);
            }
            else
            {
                obj = default;
                return (ZenResult.NoData, 0);
            }
        }

        internal int Clear()
        {
            this.@object = null;
            var memoryDifference = -this.memoryOwner.Memory.Length;
            this.memoryOwner = this.memoryOwner.Return();
            this.memoryOwnerAvailable = false;
            return memoryDifference;
        }

        private ITinyhandSerialize? @object;
        private bool memoryOwnerAvailable;
        private ByteArrayPool.ReadOnlyMemoryOwner memoryOwner;
    }
}
