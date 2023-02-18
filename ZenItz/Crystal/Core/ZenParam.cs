// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz.Crystal.Core;

public record ZenStartParam(bool ForceStart = false, ZenStartQueryDelegate? QueryDelegate = null, bool FromScratch = false)
{
    public Task<bool> Query(ZenStartResult query, string[]? list = null)
        => this.QueryDelegate == null || this.ForceStart ? Task.FromResult(true) : this.QueryDelegate(query, list);
}

public record ZenStopParam(bool RemoveAll = false);

public enum ZenStartResult
{
    Success,
    ZenFileNotFound,
    ZenFileError,
    ZenDirectoryNotFound,
    ZenDirectoryError,
    NoDirectoryAvailable,
}

public delegate Task<bool> ZenStartQueryDelegate(ZenStartResult query, string[]? list);
