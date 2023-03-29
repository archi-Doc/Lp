﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Data.SqlTypes;

namespace CrystalData;

public record CrystalStartParam(bool ForceStart = false, CrystalStartQueryDelegate? QueryDelegate = null, bool FromScratch = false)
{
    public static readonly CrystalStartParam Default = new(true);

    public Task<bool> Query(CrystalStartResult query, string[]? list = null)
        => this.QueryDelegate == null || this.ForceStart ? Task.FromResult(true) : this.QueryDelegate(query, list);
}

public record CrystalStopParam(bool RemoveAll = false)
{
    public static readonly CrystalStopParam Default = new(false);
}

public enum CrystalStartResult
{
    Success,
    FileNotFound,
    FileError,
    DirectoryNotFound,
    DirectoryError,
    NoDirectoryAvailable,
    DeserializeError,
}

public delegate Task<bool> CrystalStartQueryDelegate(CrystalStartResult query, string[]? list);
