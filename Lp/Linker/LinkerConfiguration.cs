// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;
using Lp.T3cs;

namespace Lp;

[TinyhandObject(ImplicitKeyAsName = true, EnumAsString = true)]
public partial record LinkerConfiguration
{
    public const string Filename = "LinkerConfiguration.tinyhand";
    public const string DefaultName = "Test linker";

    public LinkerConfiguration()
    {
    }

    [KeyAsName]
    [DefaultValue(DefaultName)]
    [MaxLength(LpConstants.MaxNameLength)]
    public partia string LinkerName { get; private set; } = string.Empty;
}
