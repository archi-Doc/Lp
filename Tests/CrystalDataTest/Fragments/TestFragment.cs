// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;

namespace LP.Fragments;

[TinyhandObject]
public partial class TestFragment : FragmentBase
{
    [Key(0)]
    // [Key(0, MarkPosition = true)]
    public string Name { get; set; } = string.Empty;
}
