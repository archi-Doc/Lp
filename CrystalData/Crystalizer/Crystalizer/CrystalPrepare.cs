// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public enum AbortOrContinue
{
    Abort,
    Continue,
}

public delegate ValueTask<AbortOrContinue> CrystalPrepareQueryDelegate(PathConfiguration configuration, CrystalResult result);

public class CrystalPrepare
{
    public static readonly CrystalPrepare ContinueAll = new();

    public static readonly CrystalPrepare New = new() { CreateNew = true, };

    public CrystalPrepare()
    {
        this.QueryDelegate = (configuration, result) => ValueTask.FromResult(AbortOrContinue.Continue); // Continue all
    }

    public bool CreateNew { get; init; } = false;

    public CrystalPrepareQueryDelegate QueryDelegate { get; init; }

    public ValueTask<AbortOrContinue> Query(PathConfiguration configuration, CrystalResult result)
        => this.QueryDelegate(configuration, result);

    public PrepareParam ToParam<TData>(Crystalizer crystalizer)
        => new PrepareParam(crystalizer, typeof(TData))
        {
            CreateNew = this.CreateNew,
            QueryDelegate = this.QueryDelegate,
        };
}
