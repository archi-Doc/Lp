﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

    public CrystalPrepare()
    {
        this.QueryDelegate = (configuration, result) => ValueTask.FromResult(AbortOrContinue.Continue); // Continue all
    }

    public CrystalPrepareQueryDelegate QueryDelegate { get; init; }

    public ValueTask<AbortOrContinue> Query(PathConfiguration configuration, CrystalResult result)
        => this.QueryDelegate(configuration, result);

    public PrepareParam ToParam<TData>(Crystalizer crystalizer)
        => new PrepareParam(crystalizer, typeof(TData))
        {
            QueryDelegate = this.QueryDelegate,
        };
}
