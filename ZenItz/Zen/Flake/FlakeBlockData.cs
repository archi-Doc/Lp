// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    public partial class Flake
    {
        public BlockDataMethods BlockData => new(this);

        public readonly struct BlockDataMethods
        {
            public BlockDataMethods(Flake flake)
            {
                this.flake = flake;
            }

            public ZenResult Set(ReadOnlySpan<byte> span)
            {
                using (var obj = this.flake.Lock<BlockData>())
                {
                    if (obj.Data is null)
                    {
                        return Flake.NullDataResult;
                    }

                    return obj.Data.Set(span);
                }
            }

            public ZenResult SetObject<T>(T @object)
                where T : ITinyhandSerialize<T>
            {
                using (var obj = this.flake.Lock<BlockData>())
                {
                    if (obj.Data is null)
                    {
                        return Flake.NullDataResult;
                    }

                    return obj.Data.SetObject(@object);
                }
            }

            public Task<ZenMemoryResult> Get()
            {
                using (var obj = this.flake.Lock<BlockData>())
                {
                    if (obj.Data is null)
                    {
                        return Task.FromResult(new ZenMemoryResult(Flake.NullDataResult));
                    }

                    return obj.Data.Get();
                }
            }

            public Task<ZenObjectResult<T>> GetObject<T>()
                where T : ITinyhandSerialize<T>
            {
                using (var obj = this.flake.Lock<BlockData>())
                {
                    if (obj.Data is null)
                    {
                        return Task.FromResult(new ZenObjectResult<T>(Flake.NullDataResult));
                    }

                    return obj.Data.GetObject<T>();
                }
            }

            private readonly Flake flake;
        }
    }
}
