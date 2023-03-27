// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.Results;

public enum AddStorageResult
{
    Success,
    Running,
    DuplicateId,
    DuplicatePath,
    WriteError,
    NoStorageKey,
}
