// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LP.Unit;

public class BuiltUnitParameter
{
    public BuiltUnitParameter()
    {
    }

    public List<Type> CommandList { get; set; }

    public ServiceProvider ServiceProvider { get; set; }
}
