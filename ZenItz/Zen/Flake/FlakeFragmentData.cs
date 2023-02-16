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
                using (var obj = this.flake.Lock<FragmentData<TIdentifier>>())
                {
                    if (obj.Data is null)
                    {
                        return Flake.NullDataResult;
                    }

                    return obj.Data.Set(fragmentId, span);
                }
            }

            public ZenResult SetObject<T>(TIdentifier fragmentId, T @object)
                where T : ITinyhandSerialize<T>
            {
                using (var obj = this.flake.Lock<FragmentData<TIdentifier>>())
                {
                    if (obj.Data is null)
                    {
                        return Flake.NullDataResult;
                    }

                    return obj.Data.SetObject(fragmentId, @object);
                }
            }

            public Task<ZenMemoryResult> Get(TIdentifier fragmentId)
            {
                using (var obj = this.flake.Lock<FragmentData<TIdentifier>>())
                {
                    if (obj.Data is null)
                    {
                        return Task.FromResult(new ZenMemoryResult(Flake.NullDataResult));
                    }

                    return obj.Data.Get(fragmentId);
                }
            }

            public Task<ZenObjectResult<T>> GetObject<T>(TIdentifier fragmentId)
                where T : ITinyhandSerialize<T>
            {
                using (var obj = this.flake.Lock<FragmentData<TIdentifier>>())
                {
                    if (obj.Data is null)
                    {
                        return Task.FromResult(new ZenObjectResult<T>(Flake.NullDataResult));
                    }

                    return obj.Data.GetObject<T>(fragmentId);
                }
            }

            public bool Remove(TIdentifier fragmentId)
            {
                using (var obj = this.flake.Lock<FragmentData<TIdentifier>>())
                {
                    if (obj.Data is null)
                    {
                        return false;
                    }

                    return obj.Data.Remove(fragmentId);
                }
            }

            private readonly Flake flake;
        }
    }
}
