// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public partial class Zen<TIdentifier>
{
    public partial class Flake
    {
        public FragmentDataMethods FragmentData => new(this);

        public readonly struct FragmentDataMethods
        {
            public FragmentDataMethods(Flake flake)
            {
                this.flake = flake;
            }

            public ZenResult Set(TIdentifier fragmentId, ReadOnlySpan<byte> span)
            {
                using (var obj = this.flake.Lock<FragmentData>())
                {
                    if (obj.Data is null)
                    {
                        return ZenResult.NoData;
                    }

                    return obj.Data.Set(fragmentId, span);
                }
            }

            public ZenResult SetObject<T>(TIdentifier fragmentId, T @object)
                where T : ITinyhandSerialize<T>
            {
                using (var obj = this.flake.Lock<FragmentData>())
                {
                    if (obj.Data is null)
                    {
                        return ZenResult.NoData;
                    }

                    return obj.Data.SetObject(fragmentId, @object);
                }
            }

            public Task<ZenMemoryResult> Get(TIdentifier fragmentId)
            {
                using (var obj = this.flake.Lock<FragmentData>())
                {
                    if (obj.Data is null)
                    {
                        return Task.FromResult(new ZenMemoryResult(ZenResult.NoData));
                    }

                    return obj.Data.Get(fragmentId);
                }
            }

            public Task<ZenObjectResult<T>> GetObject<T>(TIdentifier fragmentId)
                where T : ITinyhandSerialize<T>
            {
                using (var obj = this.flake.Lock<FragmentData>())
                {
                    if (obj.Data is null)
                    {
                        return Task.FromResult(new ZenObjectResult<T>(ZenResult.NoData));
                    }

                    return obj.Data.GetObject<T>(fragmentId);
                }
            }

            private readonly Flake flake;
        }
    }
}
