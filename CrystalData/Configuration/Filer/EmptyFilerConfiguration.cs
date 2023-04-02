// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public record EmptyFilerConfiguration : FilerConfiguration
{
    public static readonly EmptyFilerConfiguration Default = new();

    public EmptyFilerConfiguration()
        : base(string.Empty)
    {
    }
}
