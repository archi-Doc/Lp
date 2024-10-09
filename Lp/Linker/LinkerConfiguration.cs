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

    [Key("LinkerName", AddProperty = "LinkerName")]
    [DefaultValue(DefaultName)]
    [MaxLength(LpConstants.MaxNameLength)]
    private string linkerName = string.Empty;
}
