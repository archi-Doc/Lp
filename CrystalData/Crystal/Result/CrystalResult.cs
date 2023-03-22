// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public enum CrystalResult
{
    Success,
    NotStarted,
    Started,
    Aborted,

    OverSizeLimit,
    OverNumberLimit,
    DatumNotRegistered,
    Deleted,
    NoDatum,
    NoData,
    NoStorage,
    NoFiler,
    NoFile,
    CorruptedData,
    SerializeError,
    DeserializeError,
    ReadError,
    WriteError,
    DeleteError,
}
