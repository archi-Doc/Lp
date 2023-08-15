﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Datum;

namespace CrystalData;

public partial record BaseData
{
    public readonly struct BlockDatumMethods
    {
        public BlockDatumMethods(BaseData baseData)
        {
            this.baseData = baseData;
        }

        public CrystalResult Set(ReadOnlySpan<byte> span)
        {
            using (var obj = this.baseData.Lock<BlockDatum>())
            {
                if (obj.Datum is null)
                {
                    return obj.Result;
                }

                return obj.Datum.Set(span);
            }
        }

        public CrystalResult SetObject<T>(T @object)
            where T : ITinyhandSerialize<T>
        {
            using (var obj = this.baseData.Lock<BlockDatum>())
            {
                if (obj.Datum is null)
                {
                    return obj.Result;
                }

                return obj.Datum.SetObject(@object);
            }
        }

        public Task<CrystalMemoryResult> Get()
        {
            using (var obj = this.baseData.Lock<BlockDatum>())
            {
                if (obj.Datum is null)
                {
                    return Task.FromResult(new CrystalMemoryResult(obj.Result));
                }

                return obj.Datum.Get();
            }
        }

        public Task<CrystalObjectResult<T>> GetObject<T>()
            where T : ITinyhandSerialize<T>
        {
            using (var obj = this.baseData.Lock<BlockDatum>())
            {
                if (obj.Datum is null)
                {
                    return Task.FromResult(new CrystalObjectResult<T>(obj.Result));
                }

                return obj.Datum.GetObject<T>();
            }
        }

        private readonly BaseData baseData;
    }
}
