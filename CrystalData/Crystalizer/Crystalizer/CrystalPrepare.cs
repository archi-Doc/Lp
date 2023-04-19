// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public delegate ValueTask<AbortOrContinue> CrystalPrepareQueryDelegate(PathConfiguration configuration, CrystalResult result);

public class CrystalPrepare
{
    public static readonly CrystalPrepare NoQuery = new();

    public static readonly CrystalPrepare New = new() { CreateNew = true, };

    // public bool ForceStart { get; protected set; }

    public bool CreateNew { get; init; } = false;

    public CrystalPrepareQueryDelegate? QueryDelegate { get; init; } = null;

    public ValueTask<AbortOrContinue> Query(PathConfiguration configuration, CrystalResult result)
        => this.QueryDelegate == null ? ValueTask.FromResult(AbortOrContinue.Continue) : this.QueryDelegate(configuration, result);

    public PrepareParam ToParam<TData>(Crystalizer crystalizer)
        => new PrepareParam(crystalizer, typeof(TData))
        {
            CreateNew = this.CreateNew,
            QueryDelegate = this.QueryDelegate,
        };
}

/*public enum CrystalStartResult
{
    Success,
    FileNotFound,
    FileError,
    DirectoryNotFound,
    DirectoryError,
    NoDirectoryAvailable,
    DeserializeError,
    NoJournal,
}*/
