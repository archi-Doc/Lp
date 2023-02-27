// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public partial class Mono<TIdentifier>
{
    public interface IMonoGroup<TMonoData> : IMonoGroup
    where TMonoData : IMonoData
    {
        void Set(in TIdentifier id, in TMonoData value);

        bool TryGet(in TIdentifier id, out TMonoData value);

        bool Remove(in TIdentifier id);
    }

    public interface IMonoGroup : ISimpleSerializable
    {
        void SetCapacity(int capacity);

        int Count();
    }
}
