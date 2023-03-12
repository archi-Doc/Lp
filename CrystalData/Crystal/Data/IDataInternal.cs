// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public interface IDataInternal
{
    ICrystalInternal CrystalInternal { get; }

    DatumConstructor Data { get; }

    CrystalOptions Options { get; }

    void DataToStorage<TDatum>(ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared)
        where TDatum : IDatum;

    Task<CrystalMemoryOwnerResult> StorageToDatum<TDatum>()
        where TDatum : IDatum;

    void DeleteStorage<TDatum>()
        where TDatum : IDatum;

    void SaveDatum(int id, bool unload);
}
