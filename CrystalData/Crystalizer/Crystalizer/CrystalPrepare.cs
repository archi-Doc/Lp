// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record CrystalPrepare(bool ForceStart = false, CrystalPrepareQueryDelegate? QueryDelegate = null, bool FromScratch = false)
{
    public static readonly CrystalPrepare Default = new(true);

    public Task<AbortOrComplete> Query(CrystalStartResult query, string[]? list = null)
        => this.QueryDelegate == null || this.ForceStart ? Task.FromResult(AbortOrComplete.Complete) : this.QueryDelegate(query, list);
}

/*public record CrystalStopParam(bool RemoveAll = false)
{
    public static readonly CrystalStopParam Default = new(false);
}*/

public enum CrystalStartResult
{
    Success,
    FileNotFound,
    FileError,
    DirectoryNotFound,
    DirectoryError,
    NoDirectoryAvailable,
    DeserializeError,
    NoJournal,
}

public delegate Task<AbortOrComplete> CrystalPrepareQueryDelegate(CrystalStartResult query, string[]? list);
