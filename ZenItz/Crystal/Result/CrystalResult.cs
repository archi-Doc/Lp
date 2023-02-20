// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public enum CrystalResult
{
    Success,
    OverSizeLimit,
    OverNumberLimit,
    Removed,
    NoData,
    NoDirectory,
    NoFile,
    InvalidCast,
    SerializeError,
    DeserializeError,
}
