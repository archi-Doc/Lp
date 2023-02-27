// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace CrystalData;

public interface InvalidDatum : IDatum
{
    const int Id = 0;

    static int IDatum.StaticId => Id;

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
