// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Crystal;

public static class BaseDataExtensions
{
    public static BaseData.FragmentDatumMethods<Identifier> FragmentDatum(this BaseData baseData)
        => new(baseData);
}
