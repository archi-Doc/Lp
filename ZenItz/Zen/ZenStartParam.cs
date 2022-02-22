// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public record ZenStartParam(string? DefaultFolder = null, string ZenFile = Zen.DefaultZenFile, string ZenBackup = Zen.DefaultZenBackup, string ZenDirectoryFile = Zen.DefaultZenDirectoryFile, string ZenDirectoryBackup = Zen.DefaultZenDirectoryBackup, bool ForceStart = false, ZenStartQueryDelegate? QueryDelegate = null)
{
    public Task<bool> Query(ZenStartResult query, string[]? list = null)
        => (this.QueryDelegate == null || this.ForceStart) ? Task.FromResult(true) : this.QueryDelegate(query, list);
}

public record ZenStopParam(string ZenFile = Zen.DefaultZenFile, string ZenBackup = Zen.DefaultZenBackup, string ZenDirectoryFile = Zen.DefaultZenDirectoryFile, string ZenDirectoryBackup = Zen.DefaultZenDirectoryBackup);

public enum ZenStartResult
{
    Success,
    AlreadyStarted,
    ZenFileNotFound,
    ZenFileError,
    ZenDirectoryNotFound,
    ZenDirectoryError,
    NoDirectoryAvailable,
}

public delegate Task<bool> ZenStartQueryDelegate(ZenStartResult query, string[]? list);
