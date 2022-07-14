// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Options;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record LPSettings
{
    public const string DefaultSettingsName = "Settings.tinyhand";

    public LPFlags Flags { get; set; } = default!;
}
