// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Crystal;

public record CrystalStartParam(bool ForceStart = false, ZenStartQueryDelegate? QueryDelegate = null, bool FromScratch = false)
{
    public Task<bool> Query(CrystalStartResult query, string[]? list = null)
        => this.QueryDelegate == null || this.ForceStart ? Task.FromResult(true) : this.QueryDelegate(query, list);
}

public record ZenStopParam(bool RemoveAll = false);

public enum CrystalStartResult
{
    Success,
    ZenFileNotFound,
    ZenFileError,
    ZenDirectoryNotFound,
    ZenDirectoryError,
    NoDirectoryAvailable,
}

public delegate Task<bool> ZenStartQueryDelegate(CrystalStartResult query, string[]? list);
