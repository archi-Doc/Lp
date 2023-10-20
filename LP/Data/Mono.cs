// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

/// <summary>
/// Monolithic data store.
/// </summary>
[TinyhandObject]
public sealed partial class Mono
{
    public Mono()
    {
    }

    [Key(0)]
    public MonoData<int, string> TestData { get; private set; } = new(1_000);
}
