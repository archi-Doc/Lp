// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.ComponentModel;
using Lp.Services;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp;

[TinyhandObject(ImplicitKeyAsName = true, EnumAsString = true)]
public partial record LinkerConfiguration : MergerConfigurationBase
{
    public const string Filename = "LinkerConfiguration.tinyhand";

    public LinkerConfiguration()
    {
    }
}
