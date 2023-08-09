// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Datum;

namespace xUnitTest.CrystalDataTest;

public interface InvalidDatum : IDatum
{
    void Test();
}

public class InvalidDatumImpl : InvalidDatum, IBaseDatum
{
    public InvalidDatumImpl()
    {
    }

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
