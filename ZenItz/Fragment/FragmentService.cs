// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Fragment;

public static class FragmentService
{
    public const int MaxFragmentSize = 1024 * 4; // 4KB
    public const int MaxFragmentNumber = 1024; // 1024

    public static TinyhandSerializerOptions SerializerOptions { get; } = TinyhandSerializerOptions.Standard;
}
