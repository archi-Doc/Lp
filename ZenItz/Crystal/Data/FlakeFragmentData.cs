// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public partial class BaseData
{
    public FragmentDataMethods<Identifier> FragmentData => new(this);

    public readonly struct FragmentDataMethods<TIdentifier>
        where TIdentifier : IEquatable<TIdentifier>, IComparable<TIdentifier>, ITinyhandSerialize<TIdentifier>
    {
        public FragmentDataMethods(BaseData baseData)
        {
            this.baseData = baseData;
        }

        public CrystalResult Set(TIdentifier fragmentId, ReadOnlySpan<byte> span)
        {
            using (var obj = this.baseData.Lock<FragmentDatum<TIdentifier>>())
            {
                if (obj.Datum is null)
                {
                    return NullDataResult;
                }

                return obj.Datum.Set(fragmentId, span);
            }
        }

        public CrystalResult SetObject<T>(TIdentifier fragmentId, T @object)
            where T : ITinyhandSerialize<T>
        {
            using (var obj = this.baseData.Lock<FragmentDatum<TIdentifier>>())
            {
                if (obj.Datum is null)
                {
                    return NullDataResult;
                }

                return obj.Datum.SetObject(fragmentId, @object);
            }
        }

        public Task<CrystalMemoryResult> Get(TIdentifier fragmentId)
        {
            using (var obj = this.baseData.Lock<FragmentDatum<TIdentifier>>())
            {
                if (obj.Datum is null)
                {
                    return Task.FromResult(new CrystalMemoryResult(NullDataResult));
                }

                return obj.Datum.Get(fragmentId);
            }
        }

        public Task<CrystalObjectResult<T>> GetObject<T>(TIdentifier fragmentId)
            where T : ITinyhandSerialize<T>
        {
            using (var obj = this.baseData.Lock<FragmentDatum<TIdentifier>>())
            {
                if (obj.Datum is null)
                {
                    return Task.FromResult(new CrystalObjectResult<T>(NullDataResult));
                }

                return obj.Datum.GetObject<T>(fragmentId);
            }
        }

        public bool Remove(TIdentifier fragmentId)
        {
            using (var obj = this.baseData.Lock<FragmentDatum<TIdentifier>>())
            {
                if (obj.Datum is null)
                {
                    return false;
                }

                return obj.Datum.Remove(fragmentId);
            }
        }

        private readonly BaseData baseData;
    }
}
