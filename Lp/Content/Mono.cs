// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp;

/// <summary>
/// Monolithic data store.
/// </summary>
[TinyhandObject]
public sealed partial class Mono
{
    public const string Filename = "Mono.tinyhand";

    public Mono()
    {
    }

    [KeyAsName]
    public MonoData<int, string> TestData { get; private set; } = new(1_000);
}
