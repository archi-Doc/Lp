// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Datum;

namespace CrystalData;

public interface IDataInternal
{
    IBigCrystal BigCrystal { get; }

    DatumRegistry DatumRegistry => this.BigCrystal.DatumRegistry;

    CrystalizerConfiguration CrystalOptions => this.BigCrystal.CrystalOptions;

    BigCrystalOptions BigCrystalOptions => this.BigCrystal.BigCrystalOptions;

    void DatumToStorage<TDatum>(ByteArrayPool.ReadOnlyMemoryOwner memoryToBeShared)
        where TDatum : IDatum;

    Task<CrystalMemoryOwnerResult> StorageToDatum<TDatum>()
        where TDatum : IDatum;

    void DeleteStorage<TDatum>()
        where TDatum : IDatum;

    void SaveDatum(ushort id, bool unload);
}
