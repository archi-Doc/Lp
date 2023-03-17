// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Datum;

namespace CrystalData;

public interface InvalidDatum : IDatum
{
    const ushort Id = 0;

    static ushort IDatum.StaticId => Id;

    void Test();
}

public class InvalidDatumImpl : InvalidDatum, IBaseDatum
{
    public InvalidDatumImpl()
    {
    }

    public int Id => BlockDatum.Id;

    public void Save()
    {
    }

    public void Test()
    {
    }

    public void Unload()
    {
    }
}
