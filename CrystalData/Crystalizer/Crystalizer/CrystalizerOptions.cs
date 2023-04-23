// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

public class CrystalizerOptions
{
    public CrystalizerOptions()
    {
        this.DefaultTimeout = TimeSpan.MinValue; // TimeSpan.FromSeconds(3);
    }

    public bool EnableLogger { get; set; }

    public string RootPath { get; set; } = string.Empty;

    // public bool AddExtension { get; init; } = false;

    public TimeSpan DefaultTimeout { get; set; }
}
