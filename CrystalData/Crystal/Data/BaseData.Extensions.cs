// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public static class BaseDataExtensions
{
    public static BaseData.BlockDatumMethods BlockDatum(this BaseData baseData)
        => new(baseData);

    public static BaseData.FragmentDatumMethods<TIdentifier> FragmentDatum<TIdentifier>(this BaseData baseData)
        where TIdentifier : IEquatable<TIdentifier>, IComparable<TIdentifier>, ITinyhandSerialize<TIdentifier>
        => new(baseData);
}
